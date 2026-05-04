### Soru 5: Kodlama Görevi (Coding Challenge)

Bir uygulama için ödeme işleme fonksiyonu geliştirmekle görevlendirildiniz. Yayıncıların ödeme talepleri var ve şirketimizin belirli para birimlerinde bakiyeleri bulunuyor. Yayıncılara, talep ettikleri tutarlara göre kendi döviz bakiyelerimizden ödeme yapacağız. Fonksiyon, şirketimizin bakiyesindeki fon tahsisini yönetmeli ve yayıncılara ödemeleri gerçekleştirmelidir.

Fonksiyon, girdi olarak mevcut bakiyeyi ve işlenecek ödeme taleplerini alır. Yeterli fon olduğunda her bir ödeme talebini mevcut bakiyeden düşmelidir. Ardından, kalan bakiyeyi ve ödenen ödeme taleplerini çıktı olarak döndürmelidir. 
İşte detaylar:

**Girdi Formatı:** Fonksiyon, '&' karakteri ile ayrılmış iki bölüm içeren tek bir metin dizesi (string) parametresi alır:
* İlk bölüm, şirket bakiyesindeki mevcut fonları `para_birimi:miktar|para_birimi:miktar|...` formatında listeler.
* İkinci bölüm, ödeme taleplerini `yayıncı_id:para_birimi:talep_edilen_miktar|yayıncı_id:para_birimi:talep_edilen_miktar|...` formatında listeler.
* Tüm `miktar` ve `talep_edilen_miktar` değerlerinin tam sayı (integer) olacağı garanti edilmektedir.
* Birinci veya ikinci bölümlerden herhangi birinin boş olabileceğini unutmayın.
* Girdinin her zaman doğru formatta olduğunu varsayabilirsiniz.

**Çıktı Formatı:** Aşağıdaki gibi formatlanmış bir metin dizesi (string) döndürün:
* Çıktı, girdiyle aynı notasyonda olmalıdır: bakiye ve ödeme kısımları aynı "&" birleştirme karakteri ile ayrılmalıdır. Hiçbir ödeme veya bakiye listelenmese bile, "&" birleştirme karakteri her zaman belirtilmelidir.
* Bakiye kısmı, para birimi adına göre alfabetik olarak sıralanmalı ve sonuç bakiyesi nota uygun olarak (`para_birimi1:kalan_miktar|para_birimi2:kalan_miktar&...`) belirtilmelidir. Ödemelerden sonra bakiye kalmasa bile, *eğer o para birimi girdide verilmiş ve destekleniyorsa*, her bir para birimi bakiye kısmında sıfır bakiye ile listelenmelidir.
* Ödenen ödeme talepleri, çıktının bakiye kısmında listelendikleri sırayla para birimlerine göre gruplandırılmalı ve bu grup içinde kesinti yapılmış (ücret düşülmüş) miktara göre artan şekilde sıralanmalıdır. Çıktıda *sadece* ödenmesi gerçekleşen talepler listelenmelidir.

**Gereksinimler:**
* *Sadece* TRY, EUR ve USD desteklenmelidir. Desteklenmeyen para birimlerine ait fonları ve ödemeleri atlayın; çıktıda desteklenmeyen para birimi bakiyesini veya ödemesini göstermeyin.
* Her para biriminin, `talep_edilen_miktar` üzerinden uygulanan bir işlem ücreti (TRY için 1, desteklenen diğer para birimleri için 2) vardır.
* İşlem ücreti, ödeme yapılmadan *önce* `talep_edilen_miktar`dan düşülmelidir (bunun sonucunda `gerçek_miktar` elde edilir).
* Ödeme talebinin karşılanabilmesi için `talep_edilen_miktar`ın işlem ücretinden yüksek olması gerekir (yani `gerçek_miktar` pozitif olmalıdır).
* `gerçek_miktar`, şirketin bakiyesinden düşülmelidir.
* Ödemelerin, her bir para birimi için `gerçek_miktar` baz alınarak artan (ascending) sırada işlendiğinden emin olun.
* Her para birimi altındaki her başarılı ödeme, `gerçek_miktar`a göre artan sırada sıralanmalıdır.
* Bir yayıncının aynı veya farklı para birimlerinde birden fazla ödeme talebi olabilir; ödemeler hiçbir şekilde birleştirilmemeli ve çıktıda ayrı ayrı görünmelidir.
* Programınız, gelecekte yeni bir para birimini desteklemeyi planlarsak buna kolayca adapte olabilmelidir.
* Temiz kod yazımı (clean coding) ve kabul edilebilir zaman karmaşıklığıyla (time complexity) çözüm üretilmesi de değerlendirilecektir.

**Örnekler**

**Örnek 1:**
* **Girdi:** `"TRY:5000|EUR:300|AZN:150&streamer1:USD:150|streamer2:EUR:100|streamer3:USD:200|streamer4:TRY:1400|streamer4:TRY:110|streamer6:AZN:10|streamer7:RUB:20|streamer16:TRY:8"`
* **Çıktı:** `"EUR:202|TRY:3485&streamer2:EUR:98|streamer16:TRY:7|streamer4:TRY:109|streamer4:TRY:1399"`

**Örnek 2:**
* **Girdi:** `"USD:276|EUR:300|TRY:1100&streamer7:USD:120|streamer2:EUR:112|streamer55:USD:200|streamer4:TRY:1000|streamer5:TRY:375"`
* **Çıktı:** `"EUR:190|TRY:726|USD:158&streamer2:EUR:110|streamer5:TRY:374|streamer7:USD:118"`

[**Coding Challenge Solution**](Program.cs)
