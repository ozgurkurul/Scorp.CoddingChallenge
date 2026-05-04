### Live Coding Stratejisi (`program.cs` Üzerinden)
İki saatlik süreçte senden sadece çalışan bir kod değil, o kodu baskı altında nasıl evrimleştirdiğini görmek isteyeceklerdir. Gönderdiğin kod gayet temiz, ancak mülakatta bu kodu "kırıp" yeniden yapmanı isteyecekleri şu senaryoların gelme ihtimali çok yüksek:

*   **1. Memory Allocation (Hafıza Tahsisi) Tuzağı:** Kodunda `input.Split(SectionSeparator)` gibi çok sayıda string manipülasyonu var. Mülakatçı, *"Eğer bu string bize ağ üzerinden 5 GB'lık bir parça halinde gelseydi sistemin RAM'i ne olurdu?"* diye sorabilir.
    *   **Çözüm Stratejisi:** Kodu `string.Split` (Heap'te sürekli yeni array yaratır) yerine, sıfır memory allocation sağlayan `ReadOnlySpan<char>` yapısına veya bellek dostu `StringReader` yapısına geçirmeni isteyebilirler.
*   **2. Concurrency (Eşzamanlılık) ve Race Condition:** *"Bu bakiye hesaplamasını tek bir string için değil de, saniyede 10.000 farklı işlemi aynı anda işleyen asenkron bir servise dönüştür."*
    *   **Çözüm Stratejisi:** `PaymentProcessor` içindeki `Dictionary` yapılarını thread-safe olan `ConcurrentDictionary`'ye çevirmen gerekecektir. Aynı yayıncının (`StreamerId`) bakiyesine iki farklı thread'in aynı anda yazmasını engellemek için `AddOrUpdate` metodunu kullanmalısın.
*   **3. SOLID ve Genişletilebilirlik:** *"Sisteme 'Crypto' ödeme tipi eklendi ve bu tipin işlem ücreti (ProcessingFee) sabit değil, o anki ağ yoğunluğuna göre dinamik hesaplanıyor."*
    *   **Çözüm Stratejisi:** `Currencies` statik sınıfındaki hardcoded yapıyı kırıp, bir `ICurrencyProcessor` arayüzü yaratman ve Dependency Injection ile sistemi yeni ödeme tiplerine (Değişime kapalı, gelişime açık - OCP) uygun hale getirmen istenir.

**Mülakat Anı İçin Altın Kural:**
Kodu yazarken asla tamamen sessiz kalma. Ekranı paylaşırken, *"Burada `List` kullanıyorum çünkü işlem sırası şimdilik önemli, ancak birazdan performans darboğazı görürsek bunu anında `HashSet`'e çevirebilirim"* gibi sesli düşünerek mimari ödünleşimlerin (trade-off) farkında olduğunu göster.

Tüm bu teknik ve kültürel cephaneliği hazırladık. Peki, iki saatlik live coding sırasında kodun bir yerinde takıldığın, beyninin durduğu veya sorunun çözümünü gerçekten o an göremediğin bir "tıkanma" anı yaşarsan (ki en tecrübeli mühendislerin bile başına gelir), mülakatçıya bu durumu nasıl yönettiğini göstermek için kullanacağın kriz iletişim stratejisini belirledin mi?