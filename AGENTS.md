# AGENTS.md — Tüm Yapay Zekalar İçin Zorunlu Talimat

Bu repo **Tags** Godot C# addon'udur: hiyerarşik tag sistemi (Unreal `GameplayTag` benzeri). Bağımsızdır; başka hiçbir addon'u bilmez.
AbilitySystem addon'u (`addons/AbilitySystem/`, Godot-AI-AbilitySystem) bu addon'u kullanır. Projede birden fazla yapay zeka çalışıyor.

**Herhangi bir şey yapmadan önce sırayla oku:**

1. [`Docs/RULES.md`](Docs/RULES.md) — bağlayıcı kurallar (AbilitySystem reposuyla ortak)
2. [`Docs/Tag.md`](Docs/Tag.md) — tag sistemi tasarımı ve kararları
3. [`Docs/PROGRESS.md`](Docs/PROGRESS.md) — yapılanlar / yapılmakta olanlar / yapılacaklar

## Hatırlatma (özet — ayrıntı RULES.md'de)
- İsimlerde **`Gameplay` ön eki yok**.
- Modüler, okunaklı; **aşırı soyutlama ve aşırı bağımlılık yok**; tek sınıf her işi yapmaz.
- **Her sınıf `addons/Tags/Core/<Sinif>/` klasöründe**, sahnesi de aynı klasörde. Projeye özel veriler (ör. `Tags.tres`) addon'a değil proje kökündeki `Resource/` klasörüne gider.
- **Sahne kuralı:** Önce `Node2D`/`Node`/`Control` ekleyip sonra script bağlamak **yasak**. Düğüm doğrudan özel sınıftan, `script = ExtResource(...)` + `metadata/_custom_type_script = "uid://..."` ile yaratılır. UID'yi `.cs.uid` dosyasından al, uydurma.
- **Performans önemli:** hot-path'te allocation/LINQ/boxing yok. Çalışma anında tag'ler `int Id` ile karşılaştırılır, string kullanılmaz.
- Mümkün olduğunca **saf C#** (`List`, `Dictionary`, `event`, `struct`); gereksiz yere Godot `Array`/`Dictionary`/`Variant`/`Signal` kullanma.
- Namespace `Tags`.
- Git komutları bu klasörde çalışır; proje kökü repo değildir. **`Test/` klasörü kullanıcıya aittir, dokunma.**
- `Docs/PROGRESS.md`'yi güncel tut; başkasının "Yapılıyor" işine dokunma.
- Kapsam dışı değişiklik yapma; kuralları kendi başına değiştirme.
