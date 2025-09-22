using Razorpay.Api;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly string key = "rzp_test_RJQk80WOKHkaXA";      // from Razorpay dashboard
    private readonly string secret = "7aENtqfwwMa6DC8Orq360dB3";

  [HttpPost("create-order")]
public IActionResult CreateOrder([FromBody] OrderRequest request)
{
    RazorpayClient client = new RazorpayClient(key, secret); // use proper key & secret

    Dictionary<string, object> options = new Dictionary<string, object>
    {
        { "amount", request.Amount * 100 },  // amount in paise
        { "currency", "INR" },
        { "payment_capture", 1 }
    };

    Order order = client.Order.Create(options);

    return Ok(new
    {
        id = order["id"].ToString(),
        amount = order["amount"],
        currency = order["currency"].ToString()
    });
}


   [HttpPost("verify")]
public IActionResult VerifyPayment([FromBody] VerifyRequest request)
{
    try
    {
        string payload = request.razorpay_order_id + "|" + request.razorpay_payment_id;

        var secretBytes = Encoding.UTF8.GetBytes(secret); // secret from Razorpay Dashboard
        using (var hmac = new HMACSHA256(secretBytes))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            if (generatedSignature == request.razorpay_signature)
            {
                return Ok(new { success = true, message = "Payment verified successfully" });
            }
            else
            {
                return Ok(new { success = false, message = "Signature mismatch" });
            }
        }
    }
    catch (Exception ex)
    {
        return BadRequest(new { success = false, message = ex.Message });
    }
}


    private string GenerateSignature(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using (var hmac = new HMACSHA256(secretBytes))
        {
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var hash = hmac.ComputeHash(payloadBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}

public class OrderRequest
{
    public int Amount { get; set; }  // Amount in rupees
}

public class RazorpayVerifyRequest
{
    [JsonPropertyName("razorpay_payment_id")]
    public string RazorpayPaymentId { get; set; }

    [JsonPropertyName("razorpay_order_id")]
    public string RazorpayOrderId { get; set; }

    [JsonPropertyName("razorpay_signature")]
    public string RazorpaySignature { get; set; }
}

public class VerifyRequest
{
    public string razorpay_payment_id { get; set; }
    public string razorpay_order_id { get; set; }
    public string razorpay_signature { get; set; }
}
