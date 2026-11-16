# Tag Sistemi — Plan

> Durum: **onaylandı, uygulanıyor**.
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
| `TagContainer` | düz C# `class` | `Core/TagContainer/` | Değişmeyen/az değişen tag kümesi (ability'nin kendi tag'leri, engellenen tag'ler vb.). |
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
- İçinde `List<Tag>` (az eleman, cache dostu). Aynı tag iki kez eklenmez.
- `Add`, `Remove`, `Clear`, `Has(tag)` (hiyerarşik), `HasExact(tag)`, `HasAny(TagContainer)`, `HasAll(TagContainer)`.
- `foreach` boxing'i olmasın diye dışarıya `Count` + indexer açılır, `IEnumerable` açılmaz.
- `FromNames(string[])`: resource'lardaki isim listesinden container üretir (yükleme anında).
- Eleman sayısı küçük olduğundan doğrusal arama yeterli. Ölçüm gerektirirse bitset'e geçilir.

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
- `[GlobalClass]`, `[Export] string[] Tags`: editörde düzenlenen liste. Godot tipi burada zorunlu (editör serileştirmesi).

### TagLoader (Autoload)
- `project.godot` → `[autoload]` içinde script olarak kayıtlı (sahne yok, export gerekmiyor).
- `_EnterTree`'de `res://Resource/Tags/Tags.tres`'i yükler; Autoload'lar ana sahneden önce ağaca girdiği için diğer sistemler hazır registry bulur.

## Editör / Veri Tarafı
- `Tag` struct'ı Godot editöründe `Export` edilemez. Resource'lar (`AbilityData`, `EffectData`) tag'leri `[Export] string[]` olarak tutar.
- Bu string'ler resource ilk kullanıldığında **bir kez** `TagContainer.FromNames` ile çevrilip önbelleğe alınır. Çalışma anında string karşılaştırması olmaz.

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
