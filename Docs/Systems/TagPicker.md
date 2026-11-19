# Tag Seçme Ağacı — Plan

> Durum: **onaylandı** (kullanıcı, 2026-11-19): T1–T5 öneriler gibi; **uygulandı**, derleme temiz (editör sınıfları DLL'de, `--import` hatasız). Editör denemesi kullanıcıda.
>
> Uygulamada netleşenler:
> - Tag isimleri tek yerden: `internal TagContainer.CollectTagNames()` (sıralı, ebeveynler dahil); `BuildTagOptions` de onu kullanır.
> - Kısmi işaret çoklu modda `TreeItem.SetIndeterminate`, tekli modda vurgu rengi (seçili tag ve ebeveynleri).
> - Ağaç açılınca yalnızca seçili tag'lerin yolu açık, gerisi kapalı; aramada eşleşenler ve ebeveynleri açılır.
> - Liste editörü yazarken boş ve tekrar eden elemanları temizler (eski dizi editöründen kalanlar).
> - Tek tag alanlarında eklenti kapalıysa düz metin kutusu görünür (eski açılır menü yalnızca `TagContainer`'da yedek).
> Karşılığı: Unreal Gameplay Tag picker (`SGameplayTagPicker`, `FGameplayTagContainerCustomization`).

## Amaç
- Tag'leri inspector'da hiyerarşik ağaçtan seçmek. Ağaçta düğümler açılıp kapanır, işaretlenerek eklenir, arama ile süzülür (ör. `State` → `Debuff` → `Stun`). Şu an her eleman ayrı bir açılır menüden seçiliyor (`TagContainer.BuildTagOptions`), tag sayısı artınca düz liste zorlaşıyor.
- Yalnızca editör (`#if TOOLS`). Çalışma anı, `TagContainer` API'si ve `Tags.tres` biçimi değişmez.

## Kararlar
| # | Konu | Seçenekler | Öneri |
|---|---|---|---|
| T1 | Kapsam | (a) Yalnızca `TagContainer`. (b) Ek olarak tek tag'lik string alanlar. Bunun için Tags bir işaret sabiti verir: `TagContainer.TagNameHint`. Alan, `_ValidateProperty` içinde `hint_string`'ini bu sabite ayarlar; inspector eklentisi bu işareti görünce ağaçlı seçiciyi gösterir. Tags, alanın kime ait olduğunu bilmez. | (b). AbilitySystem tarafında `CueEntry.TagName`, `Magnitude.CallerTagName` ve planlanan ASC `TeamName` bu işarete geçer. |
| T2 | Görünüm | Inspector'da seçili tag'ler liste olarak görünür; her birinin yanında kaldır düğmesi ve altta "Düzenle…" düğmesi olur. Ağaç açılır pencerede (`PopupPanel`): üstte arama kutusu, onay kutulu `Tree`, "Tümünü aç/kapa". Tek tag alanında seçili isim ve bir düğme görünür; ağaçta tek seçim yapılır (seçince kapanır), ayrıca "Temizle" düğmesi olur. | Bu. |
| T3 | Değişiklik ne zaman yazılır | (a) Her işaretlemede hemen (`EmitChanged`; editörün geri al/yinele'si kendiliğinden çalışır). (b) Pencerenin "Tamam" düğmesiyle. | (a), Unreal gibi. |
| T4 | Ebeveyn seçimi | Ebeveyn tag da seçilebilir ve çocuklarını otomatik seçmez, çünkü ebeveyn ayrı bir tag'dir. Seçili bir çocuğu olan kapalı düğüm ayrıca vurgulanır (kısmi işaret), içindeki seçim kapalıyken de görülsün. | Bu. |
| T5 | Listede olmayan tag | `Tags.tres`'ten silinmiş bir tag inspector'da uyarı rengiyle gösterilir ve kaldırılabilir; ağaçta görünmez. Ağaçtan yeni tag eklemek (`Tags.tres`'e yazıp kaydetmek, Unreal "Add New Tag") bu planın dışında. | Bu; yeni tag ekleme IDEAS'a. |

## Ekler (kullanıcı isteği, 2026-11-19) — uygulandı, derleme temiz, editör headless hatasız açılıyor; editör denemesi kullanıcıda
| # | Konu | Karar |
|---|---|---|
| T6 | Yeni tag | Ağacın üstünde bir kutu ve "Add" düğmesi (Enter da çalışır). İsim `TagRegistry` kuralıyla denetlenir (boşluk, `,`, `:`, baştaki/sondaki/çift nokta yok) ve zaten varsa (ebeveyn olarak da) reddedilir; hata kutunun altında yazar. Geçerliyse `Tags.tres`'e eklenip kaydedilir (dosya yoksa yaratılır), ağaç yenilenir. Çoklu modda yeni tag seçilir, tekli modda alınır. T5'teki "yeni tag ekleme bu planın dışında" kararının yerine geçer. Yeniden adlandırma ve silme yine dışarıda. |
| T7 | Boş `TagContainer` alanı | Resource tipli her `TagContainer` export'u (ör. `Ability.AssetTags`) ağaçlı editörle gösterilir, alan boşken (`<empty>`) de. İlk tag seçilince yeni (gömülü) bir `TagContainer` yaratılır; tag sayısı sıfıra inince alan yeniden boş (null) olur. Her değişiklik alanın kendisine yazılır, geri al/yinele çalışır. Ayrı bir `.tres` dosyasına bağlı (paylaşılan) container yerinde düzenlenir ve dosyası kaydedilir; bu durumda geri al yoktur, alan "shared" notuyla gösterilir. Bu alanlarda Godot'un kendi resource seçicisi (dosyadan yükle, benzersiz yap) artık görünmez. |
| T8 | Konum | Ağaç penceresi düzenlenen alanın hemen altında, en az alanın genişliğinde açılır; ekranın altına sığmazsa üstünde. |
| T9 | Görünüm | Satır arası boşluk ve girinti büyür, ilişki çizgileri; her satırın arkasında gri bant (yazı renginden saydam, açık/koyu temada); kök tag'ler kalın; seçili tag'ler vurgu renginde; arama ikonu ve yanında tek aç/kapa düğmesi (bir basış hepsini açar, bir basış kapatır); altta "N selected" ve "Clear" (çoklu); tag yoksa yol gösteren boş durum yazısı. Tekli modda tıklama (veya Enter) seçer, ok tuşları seçmeden gezer. Inspector'da `TagContainer` editörü alanın adının yanında durur (alt satıra geçmez); ayrı bir düğme yok, editörün alanına (kaldır düğmeleri dışında) tıklamak ağacı açar. Not: `TreeItem.SetCellMode` hücrenin yazısını siler, yazı moddan sonra verilir. |

## Sınıflar (hepsi `#if TOOLS`, `[Tool]`)
| Sınıf | Tür | Görev |
|---|---|---|
| `TagInspectorPlugin` | `EditorInspectorPlugin` | Her `TagContainer` alanını (`PropertyHint.ResourceType` + `"TagContainer"`, boşken de), `TagContainer.InitialTags`'i ve `TagNameHint` işaretli string alanları yakalar, kendi editörünü koyar. |
| `TagContainerProperty` | `EditorProperty` | Seçili tag listesi, kaldır ve "Edit tags…" düğmeleri; diziyi temiz yazar (boş ve tekrar eden eleman yok). `EditsResource` açıkken alanı container olarak yazar (T7). |
| `TagNameProperty` | `EditorProperty` | Tek tag: isim, seçici düğmesi, temizle. |
| `TagTreePopup` | `PopupPanel` | Arama ve `Tree`. Çoklu modda onay kutuları, tekli modda seçim. Ağaç her açılışta `TagList`'ten kurulur, ebeveynler isimlerden türetilir (`TagRegistry` ile aynı kural). |

- `TagsPlugin`: `_EnterTree`'de `AddInspectorPlugin`, `_ExitTree`'de `RemoveInspectorPlugin`.
- `TagContainer`: `public const string TagNameHint`. Mevcut açılır menü yedek olarak kalır; eklenti kapalıysa o görünür.

## Bağımlılıklar
```
TagsPlugin ──► TagInspectorPlugin ──► TagContainerProperty, TagNameProperty ──► TagTreePopup ──► TagList, TagRegistry (isim kuralı)
```
- Tags yine hiçbir addon'u bilmez. AbilitySystem yalnızca `TagContainer.TagNameHint` sabitini kullanır (ayrı commit, AbilitySystem reposunda).

## Performans
- Yalnızca editörde; çalışma anına kod girmez. Ağaç pencere açılınca kurulur, inspector her çizildiğinde kurulmaz.

## Test
- Headless test edilemez (inspector eklentisi). Derleme kontrolü yapılır, kullanıcı editörde dener:
  - Çoklu seçim, ebeveyn seçimi, kısmi işaret, arama.
  - Geri al/yinele.
  - Silinmiş tag'in gösterilmesi.
  - Tek tag alanları (`CueEntry`, `Magnitude`).
  - Eklenti kapalıyken eski açılır menü.

## Uygulama Sırası
1. `TagTreePopup`.
2. `TagContainerProperty`, `TagNameProperty`.
3. `TagInspectorPlugin`, `TagsPlugin` kaydı, `TagNameHint`.
4. AbilitySystem alanlarının işarete geçmesi.
5. Doküman: `Tag.md` (Editör / Veri Tarafı), AbilitySystem `IDEAS.md`.

## Bu Planın Dışında (IDEAS)
Ağaçtan tag yeniden adlandırma ve silme (kullanan resource'ları da güncellemek gerekir), tag başına açıklama ve ipucu.
