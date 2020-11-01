using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class PaymentController : Controller
    {
        private const string _key = "YOUR_KEY";
        private const string _secret = "YOUR_SECRET";

        public ViewResult Registration()
        {
            var model = new RegistrationModel() { Amount = 500 };
            return View(model);
        }

        public ViewResult Payment(RegistrationModel registration)
        {
            OrderModel order = new OrderModel()
            {
                OrderAmount = registration.Amount,
                Currency = "INR",
                Payment_Capture = 1,    // 0 - Manual capture, 1 - Auto capture
                Notes = new Dictionary<string, string>()
                {
                    { "note 1", "first note while creating order" }, { "note 2", "you can add max 15 notes" }
                }
            };
            var orderId = CreateOrder(order);

            RazorPayOptionsModel razorPayOptions = new RazorPayOptionsModel()
            {
                Key = _key,
                AmountInSubUnits = order.OrderAmountInSubUnits,
                Currency = order.Currency,
                Name = "Skynet",
                Description = "for Dotnet",
                ImageLogUrl = "",
                OrderId = orderId,
                ProfileName = registration.Name,
                ProfileContact = registration.Mobile,
                ProfileEmail = registration.Email,
                Notes = new Dictionary<string, string>()
                {
                    { "note 1", "this is a payment note" }, { "note 2", "here also, you can add max 15 notes" }
                }
            };
            return View(razorPayOptions);
        }

        private string CreateOrder(OrderModel order)
        {
            try
            {
                RazorpayClient client = new RazorpayClient(_key, _secret);
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", order.OrderAmountInSubUnits);
                options.Add("currency", order.Currency);
                options.Add("payment_capture", order.Payment_Capture);
                options.Add("notes", order.Notes);

                Order orderResponse = client.Order.Create(options);
                var orderId = orderResponse.Attributes["id"].ToString();
                return orderId;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public ViewResult AfterPayment()
        {
            var paymentStatus = Request.Form["paymentstatus"].ToString();
            if (paymentStatus == "Fail")
                return View("Fail");

            var orderId = Request.Form["orderid"].ToString();
            var paymentId = Request.Form["paymentid"].ToString();
            var signature = Request.Form["signature"].ToString();

            var validSignature = CompareSignatures(orderId, paymentId, signature);
            if (validSignature)
            {
                ViewBag.Message = "Congratulations!! Your payment was successful";
                return View("Success");
            }
            else
            {
                return View("Fail");
            }   
        }

        private bool CompareSignatures(string orderId, string paymentId, string razorPaySignature)
        {
            var text = orderId + "|" + paymentId;
            var secret = _secret;
            var generatedSignature = CalculateSHA256(text, secret);
            if (generatedSignature == razorPaySignature)
                return true;
            else
                return false;
        }

        private string CalculateSHA256(string text, string secret)
        {
            string result = "";
            var enc = Encoding.Default;
            byte[]
            baText2BeHashed = enc.GetBytes(text),
            baSalt = enc.GetBytes(secret);
            System.Security.Cryptography.HMACSHA256 hasher = new HMACSHA256(baSalt);
            byte[] baHashedText = hasher.ComputeHash(baText2BeHashed);
            result = string.Join("", baHashedText.ToList().Select(b => b.ToString("x2")).ToArray());
            return result;
        }

        public ViewResult Capture()
        {
            return View();
        }

        public ViewResult CapturePayment(string paymentId)
        {
            RazorpayClient client = new RazorpayClient(_key, _secret);
            Razorpay.Api.Payment payment = client.Payment.Fetch(paymentId);
            var amount = payment.Attributes["amount"];
            var currency = payment.Attributes["currency"];

            Dictionary<string, object> options = new Dictionary<string, object>();
            options.Add("amount", amount);
            options.Add("currency", currency);
            Razorpay.Api.Payment paymentCaptured = payment.Capture(options);
            ViewBag.Message = "Payment capatured!";
            return View("Success");
        }
    }
}
