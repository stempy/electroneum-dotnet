# Electroneum C# DotNetCore REST api Library

This is a .NETStandard 2.0 C# port of the ETN Instant Payment API from [ETN Rest API Guide](https://community.electroneum.com/t/using-the-etn-instant-payment-api/121) and [PHP Vendor Lib](https://github.com/electroneum/vendor-php). 

There is a nuget package for the libray at [Electroneum Nuget](https://www.nuget.org/packages/ElectroneumApiClient/)

Nuget into project:
```
Install-Package ElectroneumApiClient
```


You will need your electroneum api key and secret

Usage:

## Payment Process
Generate QRCode Url with payment amount and currency, this returns the qr image for scanning with mobile app. If you have a webhook setup with the api then review Webhook below.

```cs
// initial vendor setup
var apiKey="your-etn-api-key";
var apiSecret="your-etn-api-secret";
var vendor = new EtnVendor(apiKey,apiSecret);

// prepare qr code from payment
var amountInCurrency = 10m; // decimal amount in your currency
var currency = "aud"; // payment currency
var outlet="your-outlet-id";
var paymentid ="abc1234567"; // payment id according to etn guidelines
var qrCodeUrl = await vendor.GetQrAsync(amountInCurrency, currency, outlet, paymentid);

// return qrCodeUrl in browser or something to render qr code image
```

## WebHook
Handle webhook as below, if all is well payloadResult will have a value, or a `VendorException` should be thrown

```cs
var requestUserAgent = "useragent header-from-request";
var signature = "ETN_SIGNATURE from request headers";

var webHookValidator= new EtnWebhookValidator(apiKey,apiSecret);
var payloadResult = await _webhookValidator.ValidateEtnWebHookPayloadAsync(userAgent, payload, signature);
if (payloadResult != null)
{
    // success result
    // log and store in database table somewhere
    _logger.LogInformation($"[Test:{isTesting}] Payment Received: timestamp:{payloadResult.TimeStamp} id:{payloadResult.PaymentId} amt:{payloadResult.Amount} customer:{payloadResult.Customer} ref:{payloadResult.Ref} ");

    // handle fulfulment process
}
```

## Running the Web Application locally without VS Code OR VS 2017
*NOTE: make sure you have the [DotNet Core](https://www.microsoft.com/net/download) runtimes installed*
Change directory into the `electroneum-dotnet\src\ElectroneumApiWebApp` directory and type the following in the command line

```cmd
set EtnVendorApiKey=yourapikey
set EtnVendorApiSecretKey=yourapivendorsecretkey
set EtnOutlet=youroutlet
dotnet run
```

Than open http://localhost:5000 in the browser

