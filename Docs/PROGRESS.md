# İlerleme Takibi — Tags

> Her AI/geliştirici: işe başlamadan önce oku, başlarken "Yapılıyor"a ekle, bitirince "Yapıldı"ya taşı.
> Format: `- [x] Açıklama — [Yapan, YYYY-MM-DD]`

## Yapıldı
- [x] Tag sistemi planı (`Docs/Tag.md`): kaynak `Tags.tres`, Autoload ile yükleme, ağ kapsam dışı — [Claude, 2026-11-16]
- [x] `Tag`, `TagRegistry`, `TagList`, `TagLoader` (Autoload), `TagContainer`, `TagCountContainer` — [Claude, 2026-11-16]
- [x] `TagContainer` Resource'a çevrildi; `InitialTags` editörde tag listesinden beslenen açılır menüyle seçiliyor — [Claude, 2026-11-16]
- [x] `TagContainer` depolama yapısı ölçülüp karara bağlandı: `List<Tag>` kalıyor — [Claude, 2026-11-17]
- [x] Bağımsız addon'a çevrildi (Godot-AI-Tags): namespace `Tags`, `plugin.cfg` + `TagsPlugin` (autoload ve `tags/tag_list_path` ayarını ekler), tag listesi projeye taşındı — [Claude, 2026-11-17]

## Yapılıyor
- (boş)

## Yapılacak
- [ ] İhtiyaç doğarsa: `TagQuery` / `TagRequirements` (karmaşık sorgular)
- [ ] İhtiyaç doğarsa: `TagCountContainer` için tag'e özel dinleyici

## Sorunlar / Notlar
- `Docs/RULES.md` AbilitySystem reposunda da aynı; değişirse ikisini birden güncelle.
- Bu reponun ilk iki commit'i eski tek-repo geçmişinden filtrelenerek kopyalandı; o commit'lerde dosyalar namespace `AbilitySystem` ve proje kökü yollarıyla durur.
