# İlerleme — Tags

> Başlarken "Yapılıyor"a `- [ ] iş — [Yapan, YYYY-MM-DD]` ekle, bitince "Yapıldı"ya tek satır taşı. Ayrıntı git geçmişinde ve `Tag.md`'de.

## Yapıldı
- `Tag`, `TagRegistry`, `TagList`, `TagLoader`, `TagContainer`, `TagCountContainer`, `TagsPlugin` (`Tag.md`)
- `TagContainer.BuildTagOptions()` public (başka addon'larda tek tag menüsü için) — [Claude, 2026-11-17]
- Tag seçme ağacı planı taslağı (`Systems/TagPicker.md`, T1–T5 kullanıcı onaylı) — [Claude, 2026-11-19]
- Tag seçme ağacı (`Systems/TagPicker.md`): `TagTreePopup`, `TagContainerProperty`, `TagNameProperty`, `TagInspectorPlugin`, `TagContainer.TagNameHint`/`CollectTagNames`; derleme temiz, editör denemesi kullanıcıda — [Claude, 2026-11-19]
- Tag ağacı ekleri (T6–T9, kullanıcı isteği): yeni tag kutusu (`Tags.tres`'e yazar), boş `TagContainer` alanları ağaçla düzenlenir (ilk tag'de yaratılır, son tag'de boşalır), ağaç alanın altında açılır, okunaklı görünüm; derleme temiz, editör denemesi kullanıcıda — [Claude, 2026-11-19]

## Yapılıyor
- (boş)

## Yapılacak
- [ ] İhtiyaç doğarsa: `TagQuery` / `TagRequirements`, tag'e özel dinleyici

## Notlar
- İlk iki commit eski tek-repo geçmişinden filtrelendi (namespace `AbilitySystem`, eski yollar).
