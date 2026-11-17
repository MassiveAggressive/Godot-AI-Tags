# Kurallar — Tags (zorunlu)

## Tasarım
- Unreal `GameplayTag` benzeri; isimlerde `Gameplay` ön eki yok.
- Bağımsız addon: başka hiçbir addon'u bilmez (onu kullananlar bilir).
- Tek sorumluluk; tek kullanımlık interface/factory/manager katmanı yok; döngüsel bağımlılık yasak.
- Performans: hot-path'te allocation, LINQ, boxing, string işlemi yok. Çalışma anında tag'ler `int Id` ile karşılaştırılır; string yalnızca yüklemede.

## Dil
- Saf C# tercih: `List`, `Dictionary`, dizi, `event`/`Action`, `struct`.
- Godot tipleri yalnızca zorunluysa (export, Resource serileştirme, Godot API); orada nedenini kısa not düş.

## Klasör
- Her sınıf `addons/Tags/<Sınıf>/<Sınıf>.cs` (+ `.cs.uid`); ara klasör yok, tek istisna `Docs/`.
- Projeye özel veri (tag listesi `Tags.tres`) addon'da değil, proje kökündeki `Resource/` altında.
- Dosya adı = sınıf adı (`PascalCase`). Yollar `res://addons/Tags/...`.

## Sahne (kritik)
Önce standart düğüm ekleyip sonra script bağlamak yasak. Düğüm doğrudan sınıfından: `script = ExtResource(...)` + `metadata/_custom_type_script = "uid://..."`. UID `.cs.uid`'den, uydurma; `.uid` dosyalarını commit et.

## Kod Stili
- Namespace `Tags`. `PascalCase` tip/metot/property, `_camelCase` private alan, `camelCase` yerel/parametre.
- `partial` yalnızca Godot sınıflarında. Editör kodu `#if TOOLS` içinde. Dosya başına tek ana tip. Yorum yalnızca "neden" için.

## İş Birliği
- `Docs/PROGRESS.md`: başlarken "Yapılıyor"a `[Yapan, tarih]` ile ekle, bitince taşı; başkasının işine dokunma.
- Tasarım kararları `Docs/Tag.md`'ye yazılır. Kuralları değiştirme (kullanıcıya sor). Kapsam dışı değişiklik yok.
- Git bu klasörde (`git -C addons/Tags`). `Test/` kullanıcıya ait.
