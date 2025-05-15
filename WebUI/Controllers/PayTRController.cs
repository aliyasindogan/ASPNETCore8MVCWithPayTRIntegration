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
        // ####################### D�ZENLEMES� ZORUNLU ALANLAR #######################
        //
        // API Entegrasyon Bilgileri - Ma�aza paneline giri� yaparak B�LG� sayfas�ndan alabilirsiniz.
        string merchant_id = "xxxxx";
        string merchant_key = "xxxxx";
        string merchant_salt = "xxxx";
        //
        // M��terinizin sitenizde kay�tl� veya form vas�tas�yla ald���n�z eposta adresi
        string emailstr = "info@siteniz.com";
        //
        // Tahsil edilecek tutar.
        int payment_amountstr = 1000 * 100;
        //
        // Sipari� numaras�: Her i�lemde benzersiz olmal�d�r!! Bu bilgi bildirim sayfan�za yap�lacak bildirimde geri g�nderilir.
        string merchant_oid = "xxxx";
        //
        // M��terinizin sitenizde kay�tl� veya form arac�l���yla ald���n�z ad ve soyad bilgisi
        string user_namestr = "xxxx";
        //
        // M��terinizin sitenizde kay�tl� veya form arac�l���yla ald���n�z adres bilgisi
        string user_addressstr = "xxxx";
        //
        // M��terinizin sitenizde kay�tl� veya form arac�l���yla ald���n�z telefon bilgisi
        string user_phonestr = "xxxx";
        //
        // Ba�ar�l� �deme sonras� m��terinizin y�nlendirilece�i sayfa
        // !!! Bu sayfa sipari�i onaylayaca��n�z sayfa de�ildir! Yaln�zca m��terinizi bilgilendirece�iniz sayfad�r!
        // !!! Sipari�i onaylayaca��z sayfa "Bildirim URL" sayfas�d�r (Bak�n�z: 2.ADIM Klas�r�).
        string merchant_ok_url = "http://sitem.com/basarili";
        //
        // �deme s�recinde beklenmedik bir hata olu�mas� durumunda m��terinizin y�nlendirilece�i sayfa
        // !!! Bu sayfa sipari�i iptal edece�iniz sayfa de�ildir! Yaln�zca m��terinizi bilgilendirece�iniz sayfad�r!
        // !!! Sipari�i iptal edece�iniz sayfa "Bildirim URL" sayfas�d�r (Bak�n�z: 2.ADIM Klas�r�).
        string merchant_fail_url = "http://sitem.com/basarisiz";
        //        
        // !!! E�er bu �rnek kodu sunucuda de�il local makinan�zda �al��t�r�yorsan�z
        // buraya d�� ip adresinizi (https://www.whatismyip.com/) yazmal�s�n�z. Aksi halde ge�ersiz paytr_token hatas� al�rs�n�z.
        string user_ip = HttpContext.Connection.RemoteIpAddress?.ToString() == "::1" ? "xx.xxx.xxx.xxx" : HttpContext.Connection.RemoteIpAddress?.ToString();

        //
        // �RNEK $user_basket olu�turma - �r�n adedine g�re object'leri �o�altabilirsiniz
        object[][] user_basket = {
            new object[] { "Dell Inspiron 3520 Intel Core i5 1235U 8GB 512GB SSD Ubuntu 15.6\" FHD 120Hz Ta��nabilir Bilgisayar", "10000.00", 1}, // 1. �r�n (�r�n Ad - Birim Fiyat - Adet)
            new object[] { "Calvin Klein Erkek T Shirt J30J320935 YAF", "1800.00", 1}, // 2. �r�n (�r�n Ad - Birim Fiyat - Adet)
            new object[] { "Abra A5 V20.4.4 15,6 Oyun Bilgisayar�", "37500.00", 1}, // 3. �r�n (�r�n Ad - Birim Fiyat - Adet)
            new object[] { "LCWAIKIKI Classic �ndigo Regular Fit Uzun Kollu Gabardin Erkek G�mlek", "750.00", 1}, // 3. �r�n (�r�n Ad - Birim Fiyat - Adet)
            };
        /* ############################################################################################ */
        // ��lem zaman a��m� s�resi - dakika cinsinden
        string timeout_limit = "30";
        //
        // Hata mesajlar�n�n ekrana bas�lmas� i�in entegrasyon ve test s�recinde 1 olarak b�rak�n. Daha sonra 0 yapabilirsiniz.
        string debug_on = "1";
        //
        // Ma�aza canl� modda iken test i�lem yapmak i�in 1 olarak g�nderilebilir.
        string test_mode = "1";
        //
        // Taksit yap�lmas�n� istemiyorsan�z, sadece tek �ekim sunacaksan�z 1 yap�n
        string no_installment = "0";
        //
        // Sayfada g�r�nt�lenecek taksit adedini s�n�rlamak istiyorsan�z uygun �ekilde de�i�tirin.
        // S�f�r (0) g�nderilmesi durumunda y�r�rl�kteki en fazla izin verilen taksit ge�erli olur.
        string max_installment = "0";
        //
        // Para birimi olarak TL, EUR, USD g�nderilebilir. USD ve EUR kullanmak i�in kurumsal@paytr.com 
        // �zerinden bilgi alman�z gerekmektedir. Bo� g�nderilirse TL ge�erli olur.
        string currency = "TL";
        //
        // T�rk�e i�in tr veya �ngilizce i�in en g�nderilebilir. Bo� g�nderilirse tr ge�erli olur.
        string lang = "";


        // G�nderilecek veriler olu�turuluyor
        NameValueCollection data = new NameValueCollection();
        data["merchant_id"] = merchant_id;
        data["user_ip"] = user_ip;
        data["merchant_oid"] = merchant_oid;
        data["email"] = emailstr;
        data["payment_amount"] = payment_amountstr.ToString();


        // Sepet i�er�i olu�turma fonksiyonu, de�i�tirilmeden kullan�labilir.
        string user_basket_json = JsonConvert.SerializeObject(user_basket);
        string user_basketstr = Convert.ToBase64String(Encoding.UTF8.GetBytes(user_basket_json));
        data["user_basket"] = user_basketstr;
        //
        // Token olu�turma fonksiyonu, de�i�tirilmeden kullan�lmal�d�r.
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
