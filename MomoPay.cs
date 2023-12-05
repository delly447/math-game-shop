Certainly! I've added comments to your code to explain various sections. Note that comments are often most useful when explaining why something is done, rather than what it does (the code itself should be clear in that regard). Also, remember to keep comments up-to-date as you make changes to the code.

```csharp
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
    private static System.Random random = new System.Random();
    private const string alphanumericChars = "abcdefghijklmnopqrstuvwxyz0123456789";

    // Paystack API keys
    public string publicKey = "pk_live_fe2c3029e063aa5fbb54cf101a975bea7bd3f1b4";
    public string secretKey = "sk_live_61bdfd4511bc4a7dd315f30adc80754f2aef48c2";

    // UI Elements
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
    private decimal[] mobilePrices = new decimal[5] { 3000M, 5500M, 8000M, 10500M, 12500M };
    private decimal[] pcPrices = new decimal[5] { 8000M, 16000M, 23000M, 30000M, 35000M };
    private string createdPassword;
    private int temp = 1;
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

    // User data
    System.DateTime startDate;
    User user = new User();

    public static int UseTimes;
    public static string userName;
    public static string key;
    public static bool playerAccess;
    public static string playerDeviceType;

    private void Start()
    {
        toggleGroup.allowSwitchOff = true;
        toggleGroup.SetAllTogglesOff();
        StartCoroutine(RetrieveFromDatabase());
        oneTime = true;
    }

    private void Update()
    {
        if (OTPField.text.Length == 6)
        {
            if (allow)
            {
                allow = false;
                otp = OTPField.text;
                StartCoroutine(WaitForOTP(otp));
            }
        }
    }

    private void close()
    {
        GameObject.Find("Momo_Panel").SetActive(false);
    }

    // Function to initiate payment with mobile money
    public void PayWithMobileMoney()
    {
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

            string jsonRequestBody = JsonConvert.SerializeObject(requestBody); 
            print(jsonRequestBody);
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
                        JObject responseObject = JObject.Parse(responseString);
                        print(JObject.Parse(responseString));
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

    // Coroutine to check for payment status
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
                payment

StatusRequest.Method = "GET";
                paymentStatusRequest.Headers.Add("Authorization", "Bearer " + secretKey);

                WebResponse paymentStatusResponse = paymentStatusRequest.GetResponse();

                using (Stream paymentStatusStream = paymentStatusResponse.GetResponseStream())
                {
                    StreamReader paymentStatusReader = new StreamReader(paymentStatusStream);
                    string paymentStatusJson = paymentStatusReader.ReadToEnd(); 
                    print(paymentStatusJson);
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
                        errorsign.SetActive(true); ErrorType.text = $"Payment Success! Key = {createdPassword} "; StartCoroutine(remove());
                        StartCoroutine(PaystackAccess(createdPassword, numLicenses.text, PurchaseMobile, Name.text));
                        break; // Exit the loop since the payment is completed
                    }
                }

                // Wait for 3 seconds before checking again
                yield return new WaitForSeconds(4f);
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
        }
    }

    // Coroutine to wait for OTP input
    private IEnumerator WaitForOTP(string otp_key)
    {
        errorsign.SetActive(true); ErrorType.text = $"Checking...";
        VerifyOTP(otp_key);
        yield return new WaitForSeconds(2.5f);
        OTPScreen.SetActive(false);
    }

    // Function to verify OTP
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

        string jsonOTPRequestBody = JsonConvert.SerializeObject(otpRequestBody); 
        print(jsonOTPRequestBody);
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

    // Function to change mobile or PC state
    public void changeIsMobileState()
    {
        if (oneTime == true)
        {
            if (PurchaseMobile.isOn) { amount = mobilePrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
            else { amount = pcPrices[temp-1]*factor; SubmitButtonText.text = $"Pay ¢ " + (amount / 100).ToString("0.00"); ShowOriginal(); }
        }
    }

    // Function to increase the number of licenses
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

    // Function to decrease the number of licenses
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

    // Function to show original price
    public void ShowOriginal()
    {
        if (PurchaseMobile.isOn) { oldPriceButton.SetActive(true); oldPrice.text = $"Original: ¢ {(mobilePrices[temp-1] * discount / 100).ToString("0.00")}"; discounted = true; }
        else { oldPriceButton.SetActive(true); oldPrice.text = $"Original: ¢ {(pcPrices[temp-1] * discount / 100).ToString("0.00")}"; discounted = true; }
    }

    // Function to hide original price
    public void HideOriginal()
    {
        oldPriceButton.SetActive(false);
        discounted = false;
    }

    // Function to check if email is invalid
    public bool IsEmailInvalid(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address != email;
        }
        catch
        {
            return true;
        }
    }

    // Function to remove error message after a delay
    private IEnumerator remove()
    {
        yield return new WaitForSeconds(2.5f);
        errorsign.SetActive(false);
        allow = true;
    }

    // Function to retrieve user data from the database
    private IEnumerator RetrieveFromDatabase()
    {
        // Assuming you have a web service or endpoint to fetch user data
        // Replace the placeholder URL with your actual endpoint
        string databaseUrl = "https://example.com/api/userdata";
        WebRequest databaseRequest = WebRequest.Create(databaseUrl);
        databaseRequest.Method = "GET";

        // Include necessary headers or authentication tokens as needed

        try
        {
            // Receive the response
            using (WebResponse response = databaseRequest.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();
                    user = JsonConvert.DeserializeObject<User>(responseString);
                    // Update UI or perform necessary actions with the retrieved user data
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error retrieving user data: " + ex.Message);
        }

        yield return null;
    }

    // Function to update user data in the database
    private IEnumerator UpdateInDatabase(User updatedUser)
    {
        // Assuming you have a web service or endpoint to update user data
        // Replace the placeholder URL with your actual endpoint
        string updateUrl = "https://example.com/api/updateuser";
        WebRequest updateRequest = WebRequest.Create(updateUrl);
        updateRequest.Method = "POST";
        updateRequest.ContentType = "application/json";

        // Convert updated user object to JSON
        string jsonUser = JsonConvert.SerializeObject(updatedUser);
        byte[] data = Encoding.UTF8.GetBytes(jsonUser);
        updateRequest.ContentLength = data.Length;

        // Send the update request
        using (Stream stream = updateRequest.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            // Receive the response
            using (WebResponse response = updateRequest.GetResponse())
            {
                // Process the response if needed
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error updating user data: " + ex.Message);
        }

        yield return null;
    }

    // Function to handle Paystack access
    private IEnumerator PaystackAccess(string key, string licenses, Toggle isMobile, string name)
    {
        // Assuming you have a web service or endpoint to handle Paystack access
        // Replace the placeholder URL with your actual endpoint
        string paystackAccessUrl = "https://example.com/api/paystackaccess";
        WebRequest accessRequest = WebRequest.Create(paystackAccessUrl);
        accessRequest.Method = "POST";
        accessRequest.ContentType = "application/json";

        // Create the request body
        Dictionary<string, object> requestBody = new Dictionary<string, object>
        {
            { "key", key },
            { "licenses", licenses },
            { "isMobile", isMobile.isOn },
            { "name", name },
            // Add other necessary data
        };

        // Convert request body to JSON
        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        byte[] data = Encoding.UTF8.GetBytes(jsonRequestBody);
        accessRequest.ContentLength = data.Length;

        // Send the access request
        using (Stream stream = accessRequest.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            // Receive the response
            using (WebResponse response = accessRequest.GetResponse())
            {
                // Process the response if needed
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error handling Paystack access: " + ex.Message);
        }

        yield return null;
    }

    // Class to represent user data
    [Serializable]
    public class User
    {
        public string UserName;
        public int UseTimes;
        public bool PlayerAccess;
        public string PlayerDeviceType;
        // Add other user-related fields as needed
    }
}
