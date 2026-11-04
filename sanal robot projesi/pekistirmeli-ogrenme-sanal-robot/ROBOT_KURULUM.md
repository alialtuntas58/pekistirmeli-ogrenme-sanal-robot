# Emir - Robot ve sensor kurulumu

Rapordaki kapsam: IP-3 (robot govdesi, Rigidbody ve hareket) ve IP-4 (isin sensoru).

## Kurulum

1. Unity'de bu klasordeki projeyi ve `Assets/Scenes/TraningScene.unity` sahnesini acin.
2. Script derlemesinin bitmesini bekleyin. Play modu kapali olmali.
3. Ust menuden **Robot Projesi > Robotu Olustur ve Sahneye Ekle** secin.
4. Arac `Assets/Prefabs/Robot.prefab` ve uc materyal olusturur. Robotu SpawnPoint'e yerlestirir, ajanin Target ve EnvironmentManager'in Robot alanlarini baglar, eksik Wall etiketini ekler. Sahneyi Ctrl+S ile kaydedin.
5. Play'e basin. Game sekmesine odaklanip W/S ile ileri/geri, A/D ile donusu deneyin. Ok tuslari da desteklenir.

Robot zaten EnvironmentManager'a atanmis ise arac ikinci robot eklemez. Mevcut Robot.prefab dosyasini yeniden yazmaz.

## Alternatif robotlari gorme

Play kapaliyken ust menuden **Robot Projesi > Robot Sec** menusunu acin:

- **1 - Klasik Tekerlekli:** ilk mavi govdeli, dort tekerlekli robot.
- **2 - Atlas Insansi:** beyaz zirh, koyu eklemler, mavi gozler; bas, govde, iki kol, iki bacak ve ayaklar. Kol/bacaklar sabittir; yuruyus animasyonu veya bacak dengesi simulasyonu yoktur. Ayni ileri/geri ve donus kontrolunu kullanir.
- **3 - Nova Kesif Robotu:** turuncu zirh, alti tekerlek, tamponlar, kamera diregi ve lidar gorunumlu ust parca. Kamera/lidar parcalari gorseldir; algilama yine RayPerceptionSensorComponent3D ile yapilir.

Secilen model SpawnPoint'e eklenir ve secilir. Onceki robot silinmez; soldaki nesne listesinde pasif halde tutulur. Sahnede sadece yeni secilen robot calisir. Ctrl+Z ile son model degisikligini geri alabilirsiniz. Model secimini Play modundan cikinca yapin, sonra Ctrl+S ile kaydedin.

Her modelin prefab dosyasi ilk seciminde uretilir: Robot.prefab, Robot_Humanoid.prefab, Robot_Explorer.prefab. Bu dosyalar varsa arac uzerlerine yazmaz. Yeni alternatiflerin kaynaklari derleme kontrolunden gecmistir; Unity'de gorsel ve Play dogrulamasi henuz yapilmamistir.

## Teknik bilesenler

- Unity primitive nesneleriyle uretilen govde, dort gorsel tekerlek ve sari Z+ on yon isareti. Harici robot modeli kullanilmaz.
- Tek kok BoxCollider, 3 kg dinamik Rigidbody, ContinuousDynamic carpisma, Freeze Rotation X/Z.
- `RobotMovement.cs`: iki surekli komutu fizik hizina ve Y ekseni donusune cevirir. Yercekimi icin dikey hizi korur. Tekerlekler gorseldir; WheelCollider simulasyonu kullanilmaz.
- `RobotObstacleAgent.cs`: hareketi RobotMovement'a iletir. Yeni Input System klavyesini destekler; Wall temasini da ceza kosuluna dahil eder.
- RayPerceptionSensorComponent3D: Obstacle/Wall/Target, her yonde 6 + merkezde 1 = 13 isin, 90 derece yari aci, 10 birim uzunluk, 0.1 yaricap. Bunlar baslangic degerleridir; deneyle optimize edilmemistir.
- Behavior: RobotAvoidance, 7 vektor gozlemi, 2 continuous action. Sensor gozlemleri ML-Agents tarafindan ayrica eklenir.
- Ilk kurulum HeuristicOnly; DecisionPeriod 5, kararlar arasinda eylem tekrari acik, MaxStep 2000. Egitim oncesi Behavior Type'i Default yapin.

## Dogrulama durumu

Kaynaklar, projedeki Unity 6000.6.6f1 ve ML-Agents kutuphanelerine karsi C# derleyicisiyle kontrol edildi. Unity Play testi ve kurulum menusunun calistirilmasi henuz tamamlanmadi; prefab ve materyaller menu ilk kez calistirilinca uretilir.

Play testinde ileri hareketin sari isaret yonunde oldugunu, robotun X/Z ekseninde devrilmedigini, duvar/engel temaslarini, hedefe varinca bolumun yenilendigini ve Scene gorunumundeki sensor isinlarini kontrol edin. Egitim basarisi veya sensor optimizasyonu icin olcum yapilmamistir.
