using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System.Net;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Proyecto26;
using UnityEngine.SceneManagement;

public class MomoPay : MonoBehaviour
{
    //public PlayerScores PlayerScores;
    private static System.Random random = new System.Random();
    private const string alphanumericChars = "abcdefghijklmnopqrstuvwxyz0123456789";

    public string publicKey = "pk_live_fe2c3029e063aa5fbb54cf101a975bea7bd3f1b4";
    public string secretKey = "sk_live_61bdfd4511bc4a7dd315f30adc80754f2aef48c2";
    //public string mobileMoneyNumber = "0551234987";
    public TMP_InputField MomoNumber;
    public ToggleGroup toggleGroup;
    public Toggle mtnToggle;
    public Toggle vodaToggle;
    public TMP_InputField Name, Email, Reference;
    public TMP_InputField numLicenses;
    public Toggle PurchaseMobile;
    public Button Pay;
    public Text SubmitButtonText;
    public GameObject errorsign;
    public Text ErrorType;
    public decimal amount = 0;
    private decimal[] mobilePrices = new decimal[5] { 3000M, 5500M, 8000M, 10500M, 12500M }; ///these vales should be fetched from firebase in the future
    private decimal[] pcPrices = new decimal[5] { 8000M, 16000M, 23000M, 30000M, 35000M };
    private string createdPassword;
    private int temp=1;
    private bool oneTime = false;
    private string otp;
    public GameObject OTPScreen;
    public TMP_InputField OTPField;
    private string reference;
    private bool allow = true;
    private decimal discount = 100;
    private decimal factor = 1;
    public Text oldPrice;
    public GameObject oldPriceButton;
    private bool discounted = false;


    //private System.Random random = new System.Random();
    System.DateTime startDate;
    User user = new User();

    public static int UseTimes;
    public static string userName;
    public static string key;
    public static bool playerAccess;
    public static string playerDeviceType;

    private void Start()
    {
        toggleGroup.allowSwitchOff = true; toggleGroup.SetAllTogglesOff(); //toggleGroup.allowSwitchOff = false;
        StartCoroutine(RetrieveFromDatabase()); //StartCoroutine(RetrieveFromDatabase());
        //StartCoroutine(CheckInternetConnectionCoroutine());
        oneTime = true;
    }

    private void Update()
    {
        if (OTPField.text.Length == 6) { if(allow){ allow = false; otp = OTPField.text; StartCoroutine(WaitForOTP(otp)); } }
    }

    private void close()
    {
        GameObject.Find("Momo_Panel").SetActive(false);
    }

    public void PayWithMobileMoney()
    {
        //amount = 20;
        if (MomoNumber.text.Length != 10) { errorsign.SetActive(true); ErrorType.text = "Check Mobile Number"; StartCoroutine(remove()); }
        else if (Name.text == "") { errorsign.SetActive(true); ErrorType.text = "Enter Name"; StartCoroutine(remove()); }
        else if (int.Parse(numLicenses.text) > 1 && (IsEmailInvalid(Email.text))) { errorsign.SetActive(true); ErrorType.text = "Enter Email"; StartCoroutine(remove()); }
        else if (mtnToggle.isOn == false && vodaToggle.isOn == false) { errorsign.SetActive(true); ErrorType.text = "Select Mobile Network"; StartCoroutine(remove()); }
        else
        {
            if (Email.text == null) { Email.text = "delly447@gmail.com"; }
            string url = "https://api.paystack.co/charge";
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.Headers.Add("Authorization", "Bearer " + secretKey);
            request.ContentType = "application/json";

            // Set up the request body
            List<object> customFields = new List<object>();
            customFields.Add(new Dictionary<string, object> {
                    {"display_name", "Mobile Money Number"},
                    {"variable_name", "mobile_money_number"},
                    {"value", MomoNumber.text}
                });

            Dictionary<string, object> metadata = new Dictionary<string, object>();
            metadata.Add("custom_fields", customFields);

            Dictionary<string, object> mobileMoney = new Dictionary<string, object>();
            mobileMoney.Add("phone", MomoNumber.text);
            if (mtnToggle.isOn) { mobileMoney.Add("provider", "mtn"); }
            else { mobileMoney.Add("provider", "vod"); }

            Dictionary<string, object> requestBody = new Dictionary<string, object> {
                    { "email", "delly447@gmail.com" },
                    { "amount", amount.ToString() },
                    { "metadata", metadata },
                    { "mobile_money", mobileMoney }
                };

            string jsonRequestBody = JsonConvert.SerializeObject(requestBody); print(jsonRequestBody);
            byte[] data = Encoding.UTF8.GetBytes(jsonRequestBody);
            request.ContentLength = data.Length;

            // Send the request
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                // Receive the response
                string responseString = null;
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream);
                        responseString = reader.ReadToEnd();
                        JObject responseObject = JObject.Parse(responseString); print(JObject.Parse(responseString));
                        bool transactionStatus = (bool)responseObject["status"];
                        if (transactionStatus)
                        {
                            reference = responseObject["data"]["reference"].ToString();
                            if (responseObject["data"]["status"].ToString()== "send_otp") { OTPScreen.SetActive(true); errorsign.SetActive(true); ErrorType.text = "Enter OTP"; StartCoroutine(remove()); }
                            else if(responseObject["data"]["status"].ToString() == "pay_offline") { StartCoroutine(KeepCheckingForPayment()); }
                            else { errorsign.SetActive(true); ErrorType.text = "An Error Occured"; StartCoroutine(remove()); }
                            
                        }
                        else
                        {
                            errorsign.SetActive(true); ErrorType.text = "Payment Error"; StartCoroutine(remove());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Print any exceptions to the console
                Debug.LogError("Error Occured: " + ex.Message); errorsign.SetActive(true); ErrorType.text = "Payment Error"; StartCoroutine(remove());
            }
        }
    }

    public IEnumerator KeepCheckingForPayment()
    {
        float elapsedTime = 0f; // Track the elapsed time
        errorsign.SetActive(true); ErrorType.text = $"Enter MM Pin on Device"; 

        while (true)
        {
            try
            {
                string paymentStatusUrl = $"https://api.paystack.co/transaction/verify/{reference}";
                WebRequest paymentStatusRequest = WebRequest.Create(paymentStatusUrl);
                paymentStatusRequest.Method = "GET";
                paymentStatusRequest.Headers.Add("Authorization", "Bearer " + secretKey);

                WebResponse paymentStatusResponse = paymentStatusRequest.GetResponse();

                using (Stream paymentStatusStream = paymentStatusResponse.GetResponseStream())
                {
                    StreamReader paymentStatusReader = new StreamReader(paymentStatusStream);
                    string paymentStatusJson = paymentStatusReader.ReadToEnd(); print(paymentStatusJson);
                    JObject paymentStatusObject = JObject.Parse(paymentStatusJson); 

                    string transactionStatus = paymentStatusObject["data"]["status"].ToString();

                    // Check if the transaction status indicates completion
                    if (transactionStatus == "success")
                    {
                        // Payment completed successfully
                        int length = 6;
                        char[] keyChars = new char[length];

                        for (int i = 0; i < length; i++)
                        {
                            int randomIndex = random.Next(alphanumericChars.Length);
                            keyChars[i] = alphanumericChars[randomIndex];
                        }
                        createdPassword = new string(keyChars);
                        //SendEmail();
                        errorsign.SetActive(true); ErrorType.text = $"Payment Success! Key = {createdPassword} "; StartCoroutine(remove());
                        StartCoroutine(PaystackAccess(createdPassword, numLicenses.text, PurchaseMobile, Name.text));
                        break; // Exit the loop since the payment is completed
                    }
                    //else { errorsign.SetActive(true); ErrorType.text = $"Transaction Failed"; StartCoroutine(remove()); }
                }

                //paymentStatusResponse.Close();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the API call
                errorsign.SetActive(true); ErrorType.text = $"Failed Transaction"; StartCoroutine(remove()); print(ex.Message);
            }

            elapsedTime += 4f; // Increase the elapsed time by 3 seconds

            // Check if 30 seconds have passed
            if (elapsedTime >= 44f)
            {
                // Transaction time has passed
                // Notify the user
                errorsign.SetActive(true); ErrorType.text = "Transaction time has passed"; StartCoroutine(remove());
                break; // Exit the loop since the transaction time has passed
            }

            // Wait for 3 seconds before checking again
            yield return new WaitForSeconds(4f);
        }
    }

    private IEnumerator WaitForOTP(string otp_key)
    {
        
        errorsign.SetActive(true); ErrorType.text = $"Checking...";
        VerifyOTP(otp_key);
        yield return new WaitForSeconds(2.5f);
        OTPScreen.SetActive(false);
        
    }

    public void VerifyOTP(string otpString)
    {
        // Construct the OTP verification request
        string otpVerificationUrl = "https://api.paystack.co/charge/submit_otp";
        WebRequest otpRequest = WebRequest.Create(otpVerificationUrl);
        otpRequest.Method = "POST";
        otpRequest.Headers.Add("Authorization", "Bearer " + secretKey);
        otpRequest.ContentType = "application/json";

        Dictionary<string, object> otpRequestBody = new Dictionary<string, object>
    {
        { "otp", otpString },
        { "reference", reference }
    };

        string jsonOTPRequestBody = JsonConvert.SerializeObject(otpRequestBody); print(jsonOTPRequestBody);
        byte[] otpData = Encoding.UTF8.GetBytes(jsonOTPRequestBody);
        otpRequest.ContentLength = otpData.Length;

        // Send the OTP verification request
        using (Stream otpStream = otpRequest.GetRequestStream())
        {
            otpStream.Write(otpData, 0, otpData.Length);
        }


        try
        {
            // Receive the OTP verification response
            string otpResponseString = null;
            using (WebResponse otpResponse = otpRequest.GetResponse())
            {
                using (Stream otpStream = otpResponse.GetResponseStream())
                {
                    StreamReader otpReader = new StreamReader(otpStream);
                    otpResponseString = otpReader.ReadToEnd();
                }
            }

            // Deserialize the OTP verification response JSON
            JObject otpJsonResponse = JObject.Parse(otpResponseString);
            bool otpStatus = otpJsonResponse["status"].ToObject<bool>(); 
            string otpMessage = otpJsonResponse["message"].ToString();

            if (otpStatus)
            {
                OTPScreen.SetActive(false);
                // OTP verification successful, check payment status
                string paymentStatus = otpJsonResponse["status"].ToString();

                if (paymentStatus == "true")
                {
                    StartCoroutine(KeepCheckingForPayment());
                }
                
                else
                {
                    errorsign.SetActive(true); ErrorType.text = "OTP Error"; StartCoroutine(remove());
                }
            }
        }
        catch (Exception ex)
        {
            errorsign.SetActive(true); ErrorType.text = "OTP Error"; StartCoroutine(remove());
        }
    }

    public void changeIsMobileState()
     {
        if (oneTime == true)
        {
            if (PurchaseMobile.isOn) { amount = mobilePrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
            else { amount = pcPrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
        }
     }
    public void Plus()
    {
        if (int.Parse(numLicenses.text) < 5)
        {
            temp = int.Parse(numLicenses.text); temp++;
            numLicenses.text = temp.ToString();
            if (PurchaseMobile.isOn) { amount=mobilePrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
            else { amount = pcPrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
        }
    }
    public void Minus()
    {
        if (int.Parse(numLicenses.text) > 1)
        {
            temp = int.Parse(numLicenses.text); temp--;
            numLicenses.text = temp.ToString();
            if (PurchaseMobile.isOn) { amount = mobilePrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
            else { amount = pcPrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
        }
    }
    public void SendEmail()
    {
        string senderEmail = "info.desseltech@gmail.com";
        string senderPassword = "Dd447321213?";
        string recipientEmail = Email.text;
        string subject = "Purchase of Ingenious Math Games";
        string body = $"Thank you for your purchase {Name.text}. Your activation key is {createdPassword}, and can be used for {numLicenses} installation(s). {System.Environment.NewLine} Thank you. {System.Environment.NewLine} Dessel Technologies";

        // Set up the SMTP client
        SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
        smtpClient.EnableSsl = true;
        smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);

        // Compose the email
        MailMessage mailMessage = new MailMessage(senderEmail, recipientEmail, subject, body);

        try
        {
            // Send the email
            smtpClient.Send(mailMessage);
            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to send email. Error message: " + ex.Message);
        }
    }

    public static bool IsEmailInvalid(string input)
    {
        // Email pattern regular expression
        string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

        // Check if the input does not match the email pattern
        return !Regex.IsMatch(input, emailPattern);
    }

    private IEnumerator remove()
    {
        yield return new WaitForSeconds(3.5f);
        errorsign.SetActive(false);
    } //remove error sign

    public IEnumerator PaystackAccess(string createdkey, string usetimes, bool forMobile, string usersName)
    {

        User MomoUser = new User();
        MomoUser.key = createdkey;
        MomoUser.UseTimes = int.Parse(usetimes);
        MomoUser.isFull = true;
        MomoUser.userName = usersName;
        if (forMobile) { MomoUser.DeviceType = "mobile"; }
        else { MomoUser.DeviceType = "PC"; }
        print(MomoUser.key + " " + MomoUser.UseTimes + " " + MomoUser.isFull + " " + userName);
        RestClient.Put("https://ingeniousmath-6aca6-default-rtdb.firebaseio.com/PasswordList/" + MomoUser.key + ".json", MomoUser);

        yield return new WaitForSeconds(4.0f);
        errorsign.SetActive(true); ErrorType.text = "Activating..."; StartCoroutine(remove());

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {

            RestClient.Get<User>("https://ingeniousmath-6aca6-default-rtdb.firebaseio.com/PasswordList/" + createdkey + ".json").Then(response =>
            {
                MomoUser = response;
            });

            yield return new WaitForSeconds(5.0f);

            if (MomoUser.key != null && MomoUser.UseTimes > 0)
            {
                //activate device
                //reduce password number by one 
                MomoUser.UseTimes--;
                RestClient.Put("https://ingeniousmath-6aca6-default-rtdb.firebaseio.com/PasswordList/" + MomoUser.key + ".json", MomoUser);


                if (MomoUser.isFull == true) { PlayerPrefs.SetInt("Activated", 1); saveDate(); }
                if (MomoUser.isFull == false) { PlayerPrefs.SetInt("Activated", 2); saveDate(); }
                SceneManager.LoadSceneAsync("MainMenu");

            }
        }
    }

    void saveDate()
    {
        PlayerPrefs.SetInt("SavedDaysLeft", 0);
        PlayerPrefs.SetInt("TotalTimesOpened", 0);
        startDate = System.DateTime.Now;
        PlayerPrefs.SetString("DateInitialized", startDate.ToString());
        print(PlayerPrefs.GetString("DateInitialized"));
    }

    private IEnumerator CheckInternetConnectionCoroutine()
    {
        int timestoShow = 3;
        while (true)
        {
            // Check if there is internet connectivity
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                errorsign.SetActive(true); ErrorType.text = "Check Internet"; StartCoroutine(remove());
            }
            else
            {
                if (discount != 0 && timestoShow>0) { errorsign.SetActive(true); ErrorType.text = $"{discount}% Discount Available!"; StartCoroutine(remove());  timestoShow--; }
            }

            // Wait for some time before checking again
            yield return new WaitForSeconds(6f);
        }
    }


    private IEnumerator RetrieveFromDatabase()
    {
        DiscountUser Disc = new DiscountUser();
        Disc.DiscName = "discount";
        Disc.discount_val = 0;
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {

            /*RestClient.Put<DiscountUser>("https://ingeniousmath-6aca6-default-rtdb.firebaseio.com/" + Disc.DiscName + ".json", Disc);

            yield return new WaitForSeconds(2.0f);*/

            RestClient.Get<DiscountUser>("https://ingeniousmath-6aca6-default-rtdb.firebaseio.com/" + Disc.DiscName + ".json").Then(response =>
            {
                Disc = response;

            });

            yield return new WaitForSeconds(2.0f);
            discount = Disc.discount_val; print($"discount amount is {discount}");
            if (discount != 0) { discounted = true; oldPriceButton.SetActive(true); }

            factor = 1 - (discount / 100);
            if (SystemInfo.deviceType != DeviceType.Handheld) { GameObject.Find("PC").GetComponent<Toggle>().isOn = true; amount = pcPrices[0] * factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
            else { PurchaseMobile.isOn = true; amount = mobilePrices[0] * factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00") ; ShowOriginal(); }
        }
        else
        {
            discount = 0;
            factor = 1 - (discount / 100);
            if (SystemInfo.deviceType != DeviceType.Handheld) { GameObject.Find("PC").GetComponent<Toggle>().isOn = true; amount = pcPrices[0] * factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
            else { PurchaseMobile.isOn = true; amount = mobilePrices[0] * factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
        }

        StartCoroutine(CheckInternetConnectionCoroutine());

    }

    private class DiscountUser
    {
        public string DiscName;
        public int discount_val;
    }
    
    private void ShowOriginal()
    {
        if (discounted == true)
        {
            oldPrice.text = $"was ¢ " + (amount / factor/100).ToString("0.00");
        }
    }

    public void closeMomoPanel()
    {
        GameObject.Find("Momo_Panel").SetActive(false);
    }
    public void closeOTP()
    {
        GameObject.Find("OTP_Panel").SetActive(false);
    }

}
