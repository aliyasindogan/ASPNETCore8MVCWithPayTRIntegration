using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WebUI.Controllers;

public class PayTRController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Message = "Your test page.";
        // ####################### DÜZENLEMESÝ ZORUNLU ALANLAR #######################
        //
        // API Entegrasyon Bilgileri - Maðaza paneline giriþ yaparak BÝLGÝ sayfasýndan alabilirsiniz.
        string merchant_id = "xxxxx";
        string merchant_key = "xxxxx";
        string merchant_salt = "xxxx";
        //
        // Müþterinizin sitenizde kayýtlý veya form vasýtasýyla aldýðýnýz eposta adresi
        string emailstr = "info@siteniz.com";
        //
        // Tahsil edilecek tutar.
        int payment_amountstr = 1000 * 100;
        //
        // Sipariþ numarasý: Her iþlemde benzersiz olmalýdýr!! Bu bilgi bildirim sayfanýza yapýlacak bildirimde geri gönderilir.
        string merchant_oid = "xxxx";
        //
        // Müþterinizin sitenizde kayýtlý veya form aracýlýðýyla aldýðýnýz ad ve soyad bilgisi
        string user_namestr = "xxxx";
        //
        // Müþterinizin sitenizde kayýtlý veya form aracýlýðýyla aldýðýnýz adres bilgisi
        string user_addressstr = "xxxx";
        //
        // Müþterinizin sitenizde kayýtlý veya form aracýlýðýyla aldýðýnýz telefon bilgisi
        string user_phonestr = "xxxx";
        //
        // Baþarýlý ödeme sonrasý müþterinizin yönlendirileceði sayfa
        // !!! Bu sayfa sipariþi onaylayacaðýnýz sayfa deðildir! Yalnýzca müþterinizi bilgilendireceðiniz sayfadýr!
        // !!! Sipariþi onaylayacaðýz sayfa "Bildirim URL" sayfasýdýr (Bakýnýz: 2.ADIM Klasörü).
        string merchant_ok_url = "http://sitem.com/basarili";
        //
        // Ödeme sürecinde beklenmedik bir hata oluþmasý durumunda müþterinizin yönlendirileceði sayfa
        // !!! Bu sayfa sipariþi iptal edeceðiniz sayfa deðildir! Yalnýzca müþterinizi bilgilendireceðiniz sayfadýr!
        // !!! Sipariþi iptal edeceðiniz sayfa "Bildirim URL" sayfasýdýr (Bakýnýz: 2.ADIM Klasörü).
        string merchant_fail_url = "http://sitem.com/basarisiz";
        //        
        // !!! Eðer bu örnek kodu sunucuda deðil local makinanýzda çalýþtýrýyorsanýz
        // buraya dýþ ip adresinizi (https://www.whatismyip.com/) yazmalýsýnýz. Aksi halde geçersiz paytr_token hatasý alýrsýnýz.
        string user_ip = HttpContext.Connection.RemoteIpAddress?.ToString() == "::1" ? "xx.xxx.xxx.xxx" : HttpContext.Connection.RemoteIpAddress?.ToString();

        //
        // ÖRNEK $user_basket oluþturma - Ürün adedine göre object'leri çoðaltabilirsiniz
        object[][] user_basket = {
            new object[] { "Dell Inspiron 3520 Intel Core i5 1235U 8GB 512GB SSD Ubuntu 15.6\" FHD 120Hz Taþýnabilir Bilgisayar", "10000.00", 1}, // 1. ürün (Ürün Ad - Birim Fiyat - Adet)
            new object[] { "Calvin Klein Erkek T Shirt J30J320935 YAF", "1800.00", 1}, // 2. ürün (Ürün Ad - Birim Fiyat - Adet)
            new object[] { "Abra A5 V20.4.4 15,6 Oyun Bilgisayarý", "37500.00", 1}, // 3. ürün (Ürün Ad - Birim Fiyat - Adet)
            new object[] { "LCWAIKIKI Classic Ýndigo Regular Fit Uzun Kollu Gabardin Erkek Gömlek", "750.00", 1}, // 3. ürün (Ürün Ad - Birim Fiyat - Adet)
            };
        /* ############################################################################################ */
        // Ýþlem zaman aþýmý süresi - dakika cinsinden
        string timeout_limit = "30";
        //
        // Hata mesajlarýnýn ekrana basýlmasý için entegrasyon ve test sürecinde 1 olarak býrakýn. Daha sonra 0 yapabilirsiniz.
        string debug_on = "1";
        //
        // Maðaza canlý modda iken test iþlem yapmak için 1 olarak gönderilebilir.
        string test_mode = "1";
        //
        // Taksit yapýlmasýný istemiyorsanýz, sadece tek çekim sunacaksanýz 1 yapýn
        string no_installment = "0";
        //
        // Sayfada görüntülenecek taksit adedini sýnýrlamak istiyorsanýz uygun þekilde deðiþtirin.
        // Sýfýr (0) gönderilmesi durumunda yürürlükteki en fazla izin verilen taksit geçerli olur.
        string max_installment = "0";
        //
        // Para birimi olarak TL, EUR, USD gönderilebilir. USD ve EUR kullanmak için kurumsal@paytr.com 
        // üzerinden bilgi almanýz gerekmektedir. Boþ gönderilirse TL geçerli olur.
        string currency = "TL";
        //
        // Türkçe için tr veya Ýngilizce için en gönderilebilir. Boþ gönderilirse tr geçerli olur.
        string lang = "";


        // Gönderilecek veriler oluþturuluyor
        NameValueCollection data = new NameValueCollection();
        data["merchant_id"] = merchant_id;
        data["user_ip"] = user_ip;
        data["merchant_oid"] = merchant_oid;
        data["email"] = emailstr;
        data["payment_amount"] = payment_amountstr.ToString();


        // Sepet içerði oluþturma fonksiyonu, deðiþtirilmeden kullanýlabilir.
        string user_basket_json = JsonConvert.SerializeObject(user_basket);
        string user_basketstr = Convert.ToBase64String(Encoding.UTF8.GetBytes(user_basket_json));
        data["user_basket"] = user_basketstr;
        //
        // Token oluþturma fonksiyonu, deðiþtirilmeden kullanýlmalýdýr.
        string Birlestir = string.Concat(merchant_id, user_ip, merchant_oid, emailstr, payment_amountstr.ToString(), user_basketstr, no_installment, max_installment, currency, test_mode, merchant_salt);
        HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchant_key));
        byte[] b = hmac.ComputeHash(Encoding.UTF8.GetBytes(Birlestir));
        data["paytr_token"] = Convert.ToBase64String(b);
        //
        data["debug_on"] = debug_on;
        data["test_mode"] = test_mode;
        data["no_installment"] = no_installment;
        data["max_installment"] = max_installment;
        data["user_name"] = user_namestr;
        data["user_address"] = user_addressstr;
        data["user_phone"] = user_phonestr;
        data["merchant_ok_url"] = merchant_ok_url;
        data["merchant_fail_url"] = merchant_fail_url;
        data["timeout_limit"] = timeout_limit;
        data["currency"] = currency;
        data["lang"] = lang;
        using (WebClient client = new WebClient())
        {
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            byte[] result = client.UploadValues("https://www.paytr.com/odeme/api/get-token", "POST", data);
            string ResultAuthTicket = Encoding.UTF8.GetString(result);
            dynamic json = JValue.Parse(ResultAuthTicket);
            if (json.status == "success")
            {
                ViewBag.Status = "success";
                ViewBag.Src = "https://www.paytr.com/odeme/guvenli/" + json.token;
            }
            else
            {
                //Response.Write("PAYTR IFRAME failed. reason:" + json.reason + "");
            }
        }
        return View();
    }
}
