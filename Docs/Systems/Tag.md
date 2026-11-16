# Tag Sistemi — Plan

> Durum: **uygulandı** (2026-11-16).
> Karşılığı: Unreal `GameplayTag`, `GameplayTagContainer`, `GameplayTagCountContainer`, `GameplayTagsManager`.

## Amaç
Hiyerarşik etiketler (`Ability.Fire.Ball`, `State.Stunned`) ile:
- Ability'lerin ne olduğunu tanımlamak,
- Engelleme / gereksinim kontrolü yapmak (`State.Stunned` varken `Ability` çalışmasın),
- Effect'lerin aktöre durum eklemesini sağlamak.

Hiyerarşi kuralı: `Ability.Fire.Ball` tag'ine sahip olan, `Ability.Fire` ve `Ability` tag'leriyle de **eşleşir** (tersi değil).

## Kararlar
| Konu | Karar |
|---|---|
| Tag kaynağı | Tüm tag'ler `Resource/Tags/Tags.tres` (`TagList`) içinde tanımlanır. |
| Başlatma | `TagLoader` Autoload düğümü oyun başında `Tags.tres`'i yükler ve `TagRegistry`'yi başlatır. |
| Ağ (replication) | Kapsam dışı. |

## Sınıflar

| Sınıf | Tür | Klasör | Sorumluluk |
|---|---|---|---|
| `Tag` | `readonly struct` | `Core/Tag/` | Tek bir etiket. İçinde sadece `int Id`. Karşılaştırma sayıyla, string yok. |
| `TagRegistry` | düz C# `static class` | `Core/TagRegistry/` | Tag isimlerini id'ye çevirir, ebeveyn zincirini tutar, hiyerarşik eşleşmeyi cevaplar. |
| `TagContainer` | Godot `Resource` (`[Tool]`) | `Core/TagContainer/` | Editörden düzenlenebilen, az değişen tag kümesi (ability'nin kendi tag'leri, engellenen tag'ler vb.). |
| `TagCountContainer` | düz C# `class` | `Core/TagCountContainer/` | Aktörün **canlı** tag durumu. Her tag için sayaç; aynı tag'i birden fazla kaynak ekleyebilir. Değişim olayları buradan çıkar. |
| `TagList` | Godot `Resource` | `Core/TagList/` | Projedeki tüm tag isimlerinin editörde tutulduğu liste. Verisi `Resource/Tags/Tags.tres`. |
| `TagLoader` | Godot `Node` (Autoload) | `Core/TagLoader/` | Oyun başında `Tags.tres`'i yükleyip `TagRegistry.Initialize` çağırır. Başka iş yapmaz. |

`TagRequirements` / `TagQuery` (karmaşık sorgular) **şimdilik yok**. Ability/Effect tarafında ihtiyaç doğarsa eklenir.

## Ayrıntılar

### Tag
- `readonly struct Tag : IEquatable<Tag>` → `int Id`. `Id == 0` → `Tag.None` (geçersiz).
- Sadece `TagRegistry` yeni tag üretir (constructor `internal`).
- `Matches(Tag parent)`: hiyerarşik eşleşme (`TagRegistry`'ye sorar).
- `MatchesExact(Tag other)`: sadece `Id` karşılaştırması.
- `ToString()`: sadece debug için isim döndürür (hot-path'te çağrılmaz).
- Boxing olmaması için `IEquatable<Tag>` + `GetHashCode() => Id`.

### TagRegistry
- `Initialize(string[] names)`: tek sefer çağrılır (`TagLoader`), sonra kilitlenir. Çalışma sırasında yeni tag eklenmez.
- Kayıt sırasında `A.B.C` → `A`, `A.B`, `A.B.C` otomatik kaydedilir. Geçersiz isimler (`""`, `.A`, `A.`, `A..B`, boşluk içeren) hata verip atlanır.
- `Tag Request(string name)`: isimden tag. Bulamazsa `Tag.None` döner + `GD.PushError`. Yükleme anında çağrılır, tick'te **çağrılmaz**.
- İç veri (id ile indekslenen düz diziler):
  - `string[] _names`
  - `int[] _parentIds` (kökün ebeveyni `0`)
  - `Dictionary<string, int> _idsByName` (sadece yükleme/istek için)
- `Matches(Tag tag, Tag parent)`: ebeveyn zincirinde yürür. Derinlik küçük (3–5), allocation yok.
- `Count`: `TagCountContainer` dizilerini boyutlandırmak için (id 0 dahil).
- `MaxDepth`: en derin tag'in seviye sayısı (`TagCountContainer` için `stackalloc` boyutu).

### TagContainer
- `[Tool]`, `[GlobalClass]` Resource. `[Export] string[] InitialTags`: editörde başlangıç tag'leri.
- Editörde her eleman **açılır menüden** seçilir: `_ValidateProperty`, `Tags.tres`'teki isimleri (ebeveynler dahil, sıralı) `PropertyHint.TypeString` + `Enum` ipucuna çevirir. Sadece editörde çalışır.
- `InitialTags` ilk kullanımda **bir kez** `List<Tag>`'e çevrilir (boş elemanlar ve tekrarlar atlanır). `InitialTags` değiştirilirse önbellek sıfırlanır.
- `Add`, `Remove`, `Clear`, `Has(tag)` (hiyerarşik), `HasExact(tag)`, `HasAny(TagContainer)`, `HasAll(TagContainer)`.
- `foreach` boxing'i olmasın diye dışarıya `Count` + indexer açılır, `IEnumerable` açılmaz.
- **Dikkat:** `.tres` olarak yüklenen bir `TagContainer` onu yükleyen herkes tarafından paylaşılır. Bu yüzden export edilen container'lar salt okunur veri gibi kullanılır; canlı durum `TagCountContainer`'da tutulur. Kodda `new TagContainer()` ile oluşturulan container serbestçe değiştirilebilir.
- **Depolama kararı: `List<Tag>` + doğrusal arama.** Alternatifler ölçüldü (Release, ns/işlem, n = container eleman sayısı):

  | n | Yapı | Has | HasExact | HasAny | Add+Remove |
  |---|---|---|---|---|---|
  | 9 | List (seçilen) | 12,3 | 3,6 | 38,9 | 7,9 |
  | 9 | HashSet (tag + ebeveyn kümesi) | 3,8 | 4,0 | 7,1 | 88,9 |
  | 9 | Bitset | 2,3 | 2,5 | 5,0 | 28,7 |
  | 65 | List (seçilen) | 85,9 | 24,3 | 265,1 | 45,7 |
  | 65 | HashSet | 3,9 | 4,0 | 8,3 | 490,1 |
  | 65 | Bitset | 2,3 | 2,5 | 5,0 | 168,7 |

  `HashSet`/bitset sorgularda hızlı ama container başına bellek ve `Add`/`Remove` maliyeti artıyor. Tag container'ları küçük (birkaç eleman) ve sorgular `TagCountContainer` üzerinden O(1) yapıldığı için en basit yapı seçildi. Container'lar büyür ve sorgu darboğaz olursa önce bitset'e geçilir.
- `HashSet` tek başına yetersiz: hiyerarşik `Has` için tüm elemanların gezilmesi gerekir (List'ten yavaş). Bu yüzden `HashSet` ancak tag + ebeveyn kümesi olarak ikili kullanılırsa işe yarar.

### TagCountContainer
- İki sayaç dizisi, tag id ile indeksli, `TagRegistry.Count` boyutunda bir kez ayrılır:
  - `int[] _explicitCounts`: doğrudan eklenen tag'ler.
  - `int[] _implicitCounts`: eklenen tag'in kendisi + tüm ebeveynleri (hiyerarşik sorgu için).
- `Add(Tag, int count = 1)` / `Remove(Tag, int count = 1)` (+ `TagContainer` alan sürümleri): ebeveyn zincirini yürüyüp `_implicitCounts`'u günceller. `Remove` sıfırın altına inmez.
- `Has(tag)` → `_implicitCounts[id] > 0`. **O(1)**.
- `HasExact(tag)` → `_explicitCounts[id] > 0`. **O(1)**.
- `GetCount(tag)` / `GetExactCount(tag)`.
- `HasAny(TagContainer)`, `HasAll(TagContainer)`: container elemanları üzerinden O(1) kontroller.
- Olaylar (C# `event`, Godot `Signal` değil). Hiyerarşik sayaç üzerinden, değişen tag ve tüm ebeveynleri için tetiklenir:
  - `event Action<Tag, int> CountChanged`: her sayaç değişiminde (yeni sayı ile).
  - `event Action<Tag, bool> PresenceChanged`: sayaç 0↔1 geçişinde (eklendi/kalktı).
- Olaylar, zincirdeki **tüm sayaçlar güncellendikten sonra** tetiklenir; dinleyici her zaman tutarlı durum görür.
- Tag'e özel dinleyici ihtiyacı doğarsa ayrıca eklenir (şimdilik yok).

### TagList (Resource)
- `[Tool]`, `[GlobalClass]`, `[Export] string[] Tags`: editörde düzenlenen liste. Godot tipi burada zorunlu (editör serileştirmesi).
- `[Tool]` gerekli: yoksa editör `Tags.tres`'i yer tutucu olarak yükler ve `TagContainer` menüsü dolmaz.
- `DefaultPath` sabiti: `res://Resource/Tags/Tags.tres`.

### TagLoader (Autoload)
- `project.godot` → `[autoload]` içinde script olarak kayıtlı (sahne yok, export gerekmiyor).
- `_EnterTree`'de `res://Resource/Tags/Tags.tres`'i yükler; Autoload'lar ana sahneden önce ağaca girdiği için diğer sistemler hazır registry bulur.

## Editör / Veri Tarafı
- `Tag` struct'ı Godot editöründe `Export` edilemez. Diğer resource'lar (`AbilityData`, `EffectData`) tag'leri `[Export] TagContainer` olarak tutar; tag'ler açılır menüden seçilir.
- İsimler container ilk kullanıldığında **bir kez** id'ye çevrilir. Çalışma anında string karşılaştırması olmaz.
- Tag isimleri `,` ve `:` içeremez (menü ipucunu bozar).

## Bağımlılıklar
```
TagLoader ──► TagList (Tags.tres)
    │
    ▼
TagRegistry ◄── Tag ◄── TagContainer
                  ▲
                  └──── TagCountContainer
```
Tag sistemi Ability/Effect/Attribute'u **bilmez**. En alt katmandır.

## StringName Neden Kullanılmadı
- Çalışma anında tag'ler `int Id` ile karşılaştırılır; bu, `StringName` karşılaştırmasından da hızlıdır ve Godot'a geçiş (interop) gerektirmez.
- String yalnızca yükleme anında kullanılır (`Initialize`, `Request`, `InitialTags` çözümü). Burada `StringName` kullanmak her istekte string → `StringName` dönüşümü (native çağrı + nesne) ekler; kazanç yok.
- Kural gereği (RULES.md §3) gereksiz Godot tipi kullanılmaz.

## Performans Özeti
- Tag = 4 byte struct, eşitlik int karşılaştırması.
- Hiyerarşik sorgu: `TagCountContainer`'da O(1), `TagContainer`'da O(n·derinlik) (n küçük).
- Tick'te string, LINQ, boxing, allocation yok (olay tetiklemede `stackalloc` kullanılır).
- Sayaç dizileri aktör başına bir kez ayrılır (500 tag ≈ 4 KB / aktör).

## Uygulama Sırası
1. `Tag`
2. `TagRegistry`
3. `TagList` (Resource) + `Resource/Tags/Tags.tres`
4. `TagLoader` (Autoload)
5. `TagContainer`
6. `TagCountContainer`
