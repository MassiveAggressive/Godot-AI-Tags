# Proje Kuralları (ZORUNLU)

Bu kurallar projeye müdahale eden **tüm yapay zekalar ve geliştiriciler** için bağlayıcıdır.
Kod yazmadan önce bu dosyayı ve çalıştığın addon'un `Docs/` klasöründeki dosyaları (`PROGRESS.md`, mimari/sistem dokümanları) oku.

> Bu dosya **Godot-AI-AbilitySystem** ve **Godot-AI-Tags** repolarında aynıdır. Değiştirirsen ikisini birden güncelle.

## 1. Amaç
Godot (C#) ile Unreal Engine GAS (Gameplay Ability System) benzeri bir **AbilitySystem** ve onun kullandığı bağımsız bir **Tags** sistemi.
- İsimlerde `Gameplay` ön eki **kullanılmaz** (`AbilitySystemComponent` olur, `GameplayAbility` olmaz).

## 2. Tasarım İlkeleri
- **Modüler ve esnek**: Her sistem kendi sınıfında, kendi script dosyasında.
- **Tek sınıf her işi yapmaz**: Tek sorumluluk. Büyüyen sınıf bölünür.
- **Okunaklı kod**: Açık isimler, kısa metotlar, gereksiz yorum yok (yalnızca "neden" açıklayan yorum).
- **Aşırı soyutlama yok**: Tek kullanımlık interface/factory/manager katmanı eklenmez.
- **Aşırı bağımlılık yok**: Sınıflar birbirini minimum düzeyde bilir. Döngüsel bağımlılık yasak.
- **Performans önemlidir**: Hot-path'te (her frame / her tick) allocation, LINQ, boxing, string işlemi, gereksiz `GetNode` yok. Mümkünse `struct`, önceden ayrılmış koleksiyonlar, nesne havuzu kullan.

## 3. Dil Kuralları
- Mümkün olduğunca **saf C#** kullan: `List<T>`, `Dictionary<K,V>`, `T[]`, C# `event`/`Action`/`Func`, `struct`, `enum`.
- Gereksizse Godot tiplerini **kullanma**: `Godot.Collections.Array/Dictionary`, `Variant`, `Callable`, `[Signal]` delegate'leri.
  - Godot tipleri sadece zorunlu olduğunda (Editor'de Export edilmesi, Resource serileştirme, Godot API çağrısı) kullanılır.
  - Zorunlu kullanım yerinde kısa bir not düş (neden gerekli).
- Godot sınıfı gerektirmeyen mantık (hesaplama, kuyruk, etiket sorgusu vb.) düz C# sınıfı olur, `Node` olmak zorunda değildir.

## 4. Addon'lar, Repolar ve Klasör Yapısı
Proje iki Godot addon'undan oluşur. **Her addon ayrı bir git reposudur**; proje kökü hiçbir repoya girmez.

| Addon | Klasör | Repo | Namespace | Bağımlılık |
|---|---|---|---|---|
| Tags | `addons/Tags/` | Godot-AI-Tags | `Tags` | Yok (bağımsız) |
| AbilitySystem | `addons/AbilitySystem/` | Godot-AI-AbilitySystem | `AbilitySystem` | `Tags` (`using Tags;`) |

- `Tags` hiçbir zaman `AbilitySystem`'i bilmez.
- Proje kökü (`project.godot`, `.csproj`, `Resource/`, `Test/`) sadece test/geliştirme projesidir; git'e girmez.
- **`Test/` klasörü kullanıcıya aittir; dokunma, taşıma, commit'leme.**
- Git komutlarını ilgili addon klasöründe çalıştır (`git -C addons/<Addon> ...`).

Addon'un içinde `Core/`, `Assets/`, `Resource/` gibi ara klasörler **yoktur**. Sınıf klasörleri doğrudan addon klasörünün altında başlar; tek istisna `Docs/` (kurallar, mimari, ilerleme).

**Kural: her sınıf kendi klasöründe, o klasör doğrudan addon klasörünün altında.**
```
addons/AbilitySystem/
  A/
    A.cs
    A.cs.uid
    A.tscn        # A sınıfından sahne varsa burada
  Docs/
  plugin.cfg
```
- Bir sınıfın sahnesi varsa **aynı klasörde** durur. Sınıfa ait görsel/ses gibi varlıklar ve addon ile gelen resource dosyaları da o sınıfın klasöründe durur.
- Resource'un **sınıfı/scripti** addon içinde kendi klasöründedir. Oyuna/projeye özel **verisi** (ör. tag listesi `Tags.tres`) proje kökündeki `Resource/` altındadır ve repoya girmez.
  ```
  addons/AbilitySystem/AbilityData/AbilityData.cs   # resource sınıfı
  Resource/Abilities/Fireball.tres                  # projeye özel resource verisi
  ```
- Addon içindeki yollar `res://addons/<Addon>/...` şeklindedir.
- Bir klasör = bir sınıf. Birden fazla alakasız sınıf aynı klasöre konmaz. (Sadece o sınıfa ait küçük yardımcı `enum`/`struct` aynı klasörde olabilir.)
- Dosya adı = sınıf adı (`PascalCase`).

## 5. Sahne Yaratma Kuralı (KRİTİK)
Önce standart düğüm (`Node2D`, `Node`, `Control`...) ekleyip sonra script bağlamak **YASAKTIR**.
Özel C# sınıflarından türetilen her düğüm `.tscn` içinde doğrudan kendi sınıfından yaratılmış olarak tanımlanır:
`[ext_resource type="Script" uid="..."]` + `metadata/_custom_type_script = "uid://..."`.

```ini
[ext_resource type="Script" uid="uid://bbf8lyuj8omp1" path="res://addons/AbilitySystem/Projectile/Projectile.cs" id="1_6p3um"]

[node name="Projectile" type="Node2D"]
script = ExtResource("1_6p3um")
metadata/_custom_type_script = "uid://bbf8lyuj8omp1"
```
- `uid`, ilgili `.cs.uid` dosyasındaki değerle **birebir** aynı olmalı. UID uydurma; `.cs.uid` dosyasını oku.
- `.cs.uid` henüz yoksa önce Godot'un üretmesini sağla (projeyi aç/build et) ya da kullanıcıdan iste.
- Yeni script/sahne eklerken `.uid` dosyalarını da commit et.

## 6. Kod Stili
- Namespace: addon'un adı (`AbilitySystem` veya `Tags`); alt namespace gerekmedikçe açma.
- `PascalCase` tip/metot/property, `_camelCase` private alan, `camelCase` yerel değişken/parametre.
- `partial` yalnızca Godot sınıflarından (`Node`, `Resource`, `EditorPlugin`...) türeyen tiplerde, zorunlu olduğu için kullanılır.
- `EditorPlugin` gibi sadece editörde çalışan kodlar `#if TOOLS` içinde yazılır.
- Tek dosyada tek ana tip.
- Null-safe ve erken `return` tercih edilir; iç içe derin `if` yazma.

## 7. Çoklu Yapay Zeka İş Birliği
- Çalışmaya başlamadan önce ilgili addon'un `Docs/PROGRESS.md` dosyasını oku; **başka bir AI'ın "Yapılıyor" altına aldığı işe dokunma**.
- Bir işe başlarken `PROGRESS.md` → "Yapılıyor" bölümüne `[AI adı/tarih]` ile ekle; bitince "Yapıldı"ya taşı.
- Mimari karar aldıysan ilgili addon'un mimari/sistem dokümanına yaz; kural çelişkisi bulursan düzeltme yerine `PROGRESS.md` → "Sorunlar / Notlar" altına not düş.
- Bu kuralları değiştirme; değişiklik gerekiyorsa kullanıcıya sor.
- Kapsam dışına çıkma: istenmeyen refactor, gereksiz dosya, spekülatif özellik ekleme.
