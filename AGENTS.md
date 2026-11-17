# AGENTS.md — Tags

Godot 4 C# addon: hiyerarşik tag sistemi (Unreal `GameplayTag` benzeri). Namespace `Tags`. Bağımsızdır, başka addon'u bilmez. Birden fazla yapay zeka çalışıyor.

## Kurallar (özet; kod/sahne yazmadan önce `Docs/RULES.md`)
- `Gameplay` ön eki yok. Tek sorumluluk; aşırı soyutlama, aşırı bağımlılık ve döngüsel bağımlılık yok.
- Her sınıf `addons/Tags/<Sınıf>/` klasöründe. Projeye özel veri (ör. `Tags.tres`) proje kökündeki `Resource/`'da.
- Sahne: düğüm doğrudan özel sınıftan yaratılır (`script` + `metadata/_custom_type_script`), UID `.cs.uid`'den.
- Hot-path'te allocation/LINQ/boxing/string yok; çalışma anında tag'ler `int Id` ile karşılaştırılır.
- Git bu klasörde çalışır. `Test/` kullanıcıya ait, dokunma. Kapsam dışı değişiklik yok, kuralları kendi başına değiştirme.

## Dokümanlar (yalnızca gerekince oku)
- `Docs/PROGRESS.md`: işe başlarken "Yapılıyor"a ekle, bitince güncelle; başkasının işine dokunma.
- `Docs/Tag.md`: tasarım ve kararlar.
