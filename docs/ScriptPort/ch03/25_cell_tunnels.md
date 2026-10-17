# 25_cell_tunnels

Оригинал: `source/maps/main/ch03/25_cell_tunnels.hps` (2408 строк, Latin-1).  
Порт: `UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs`, `RTXMnesia.Levels.Ch03.CellTunnels`.  
Атрибут: `HplMapScript("maps/main/ch03/25_cell_tunnels.map")`.  
Сцена: `Assets/AmnesiaImported/Levels/maps/main/ch03/25_cell_tunnels.unity` (вне git).  
Дата: 2026-10-16. Статус: готов — порт и проверки главы завершены. Полное прохождение ограничено недостающими механиками движка.

## Функции

Все 134 исходные функции сохранены в исходном порядке, включая отладку, неиспользуемые обработчики и концовку.

| Оригинал | Порт | Статус | Сигнатура |
|---|---|---|---|
| `source/maps/main/ch03/25_cell_tunnels.hps:10` | `CellTunnels.PlayEffectVoice` | готов | `void (string  asEntryBase,int alStartIdx, int alEndIdx, string  asCallback)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:23` | `CellTunnels.PlayFlashbackVoice` | готов | `void (string  asEntryBase,int alStartIdx, int alEndIdx, string  asCallback)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:45` | `CellTunnels.TimerBlackOut` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:180` | `CellTunnels.InteractPrisonDoor` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:187` | `CellTunnels.UseKeyOnCellDoor` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:201` | `CellTunnels.CollideWriggleBarBreak` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:233` | `CellTunnels.CollideWriggleBarBreak2` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:273` | `CellTunnels.InteractLooseBricks` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:282` | `CellTunnels.TimerLooseBricks` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:289` | `CellTunnels.UseBarOnBricks` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:307` | `CellTunnels.UseHCOnBricks` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:329` | `CellTunnels.TimerHammer` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:364` | `CellTunnels.UseBarOnWall` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:372` | `CellTunnels.InteractBendBar` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:378` | `CellTunnels.CollideBarBendDone` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:406` | `CellTunnels.TimerExtraBrickPush` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:416` | `CellTunnels.InteractWoodPillar` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:429` | `CellTunnels.CollideCollapseWall` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:464` | `CellTunnels.TimerDistantHowl` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:478` | `CellTunnels.TimerStopRadialBlur` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:484` | `CellTunnels.TimerScareReaction` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:500` | `CellTunnels.CollideEscapeCell` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:512` | `CellTunnels.TimerCollapseWall` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:545` | `CellTunnels.CollideObjectInWellWater` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:562` | `CellTunnels.CollideRopeInteraction` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:573` | `CellTunnels.CollideRopeDown` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:603` | `CellTunnels.UseBucketOnRope` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:624` | `CellTunnels.UseFilledBucketOnRope` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:632` | `CellTunnels.InteractFilledBucket` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:654` | `CellTunnels.InteractPipeHole` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:661` | `CellTunnels.UseBucketOnPipe` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:685` | `CellTunnels.BucketPourWater` | готов | `void (string  asStickyArea, string  asBodyName)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:698` | `CellTunnels.TimerFlushWater` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:744` | `CellTunnels.PickUpKey` | готов | `void (string  asEntity, string  asType)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:760` | `CellTunnels.UseBarOnPipe` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:768` | `CellTunnels.CollidePushKey` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:782` | `CellTunnels.TimerPushedKey` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:795` | `CellTunnels.InteractBarInPipe` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:808` | `CellTunnels.InteractRoomDoor` | готов | `void (string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:814` | `CellTunnels.UseKeyOnDoor` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:834` | `CellTunnels.CollideGuardianTease1` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:839` | `CellTunnels.CollideGuardianTease2` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:847` | `CellTunnels.CollideGuardianTease3` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:857` | `CellTunnels.CollideWaterLurkerTease` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:868` | `CellTunnels.TimerWaterLurkerRoar` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:875` | `CellTunnels.TimerWaterLurkerSplash` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:883` | `CellTunnels.CollideCreepMusicTease` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:890` | `CellTunnels.CollideLowRumbleTease` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:905` | `CellTunnels.TimerFadeOutRumbleTeaseLoop` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:915` | `CellTunnels.CollideRoarTease` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:922` | `CellTunnels.CollideGuardRumbleTease` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:933` | `CellTunnels.CollideScreamTease` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:941` | `CellTunnels.TimerStopRumbleSound` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:971` | `CellTunnels.GuardianGroundEffect` | готов | `void (string  asArea, bool abPlaySound)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:984` | `CellTunnels.SetLumpActive` | готов | `void (int alLumpIdx, bool abX, float afFadeMul, float afDelayToEvent)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1049` | `CellTunnels.TimerGuardianExtraSlime` | готов | `void (string  asName)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1057` | `CellTunnels.CollideStartGuardian` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1103` | `CellTunnels.CheckPoint01` | готов | `void (string  asName, int alCount)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1167` | `CellTunnels.CollideAreaGuardianKill` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1187` | `CellTunnels.ResetGuardianSequence` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1287` | `CellTunnels.FadeOutGuardianLight` | готов | `void (string  asName)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1292` | `CellTunnels.FadeLightToBlue` | готов | `void (string  asName)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1302` | `CellTunnels.TimerGuardian` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1329` | `CellTunnels.EventStep1` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1342` | `CellTunnels.FadeInGuardianLight` | готов | `void (string  asName)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1351` | `CellTunnels.EventStep2` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1354` | `CellTunnels.EventStep4` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1358` | `CellTunnels.EventStep6` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1361` | `CellTunnels.EventStep7` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1364` | `CellTunnels.EventStep8` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1368` | `CellTunnels.EventStep11` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1372` | `CellTunnels.EventStep12` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1376` | `CellTunnels.EventStep14` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1380` | `CellTunnels.EventStep18` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1384` | `CellTunnels.EventStep21` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1388` | `CellTunnels.EventStep22` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1392` | `CellTunnels.EventStep24` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1396` | `CellTunnels.EventStep26` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1400` | `CellTunnels.EventStep27` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1404` | `CellTunnels.EventStep28` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1409` | `CellTunnels.EventStep29` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1414` | `CellTunnels.EventStep32` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1422` | `CellTunnels.EventStep34` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1427` | `CellTunnels.EventStep36` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1432` | `CellTunnels.EventStep37` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1436` | `CellTunnels.EventStep38` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1440` | `CellTunnels.EventStep40` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1444` | `CellTunnels.EventStep41` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1448` | `CellTunnels.EventStep42` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1452` | `CellTunnels.EventStep43` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1459` | `CellTunnels.EventStep44` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1463` | `CellTunnels.EventStep45` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1467` | `CellTunnels.EventStep47` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1471` | `CellTunnels.EventStep49` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1475` | `CellTunnels.EventStep50` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1479` | `CellTunnels.EventStep51` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1486` | `CellTunnels.EventStep53` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1490` | `CellTunnels.EventStep55` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1494` | `CellTunnels.EventStep56` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1498` | `CellTunnels.EventStep58` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1505` | `CellTunnels.EventStep9` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1519` | `CellTunnels.TimerDelayedDoorImpulse01` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1528` | `CellTunnels.EventStep10` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1545` | `CellTunnels.EventStep16` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1564` | `CellTunnels.TimerDelayedDoorImpulse02` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1573` | `CellTunnels.EventStep59` | готов | `void (string asX)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1584` | `CellTunnels.TimerPutOutTorches` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1609` | `CellTunnels.CollideAreaBeginRumble` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1620` | `CellTunnels.CollideAreaCaveIn` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1663` | `CellTunnels.TimerStopRockRumble` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1677` | `CellTunnels.CollideAfterCaveIn` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1696` | `CellTunnels.UseHCOnLockedObject` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1700` | `CellTunnels.UseHCOnLockedObject02` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1704` | `CellTunnels.UseBarOnLockedObject` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1712` | `CellTunnels.UseGlassContainerOnRope` | готов | `void (string  asItem, string  asEntity)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1720` | `CellTunnels.CollideGruntOff` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1739` | `CellTunnels.CollideInCell` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1747` | `CellTunnels.StopGuardianCountDown` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1756` | `CellTunnels.TimerGuardianInCell` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1829` | `CellTunnels.TimerGuradianKillCell` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1836` | `CellTunnels.TimerRadialOffCell` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1853` | `CellTunnels.StartAlexanderEnding` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1883` | `CellTunnels.ResumeAlexEnding` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:1890` | `CellTunnels.TimerAlexEnding` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2039` | `CellTunnels.TimerEndSanityDamage` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2053` | `CellTunnels.RemorseVoiceOver` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2058` | `CellTunnels.AlexEndWordsVoiceOver` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2073` | `CellTunnels.TimerRandAmbSound` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2089` | `CellTunnels.CollideAreaCellEvent` | готов | `void (string  asParent, string  asChild, int alState)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2111` | `CellTunnels.TimerCellEvent` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2148` | `CellTunnels.TimerScareReaction02` | готов | `void (string  asTimer)` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2155` | `CellTunnels.OnStart` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2373` | `CellTunnels.OnEnter` | готов | `void ()` |
| `source/maps/main/ch03/25_cell_tunnels.hps:2395` | `CellTunnels.OnLeave` | готов | `void ()` |

## Данные и отличия от оригинала

- Поля `glGuardianSteps` (:957), `gfFadeTime` (:958), `glDualPathStartIdx`/`glDualPathEndIdx` (:961–962), `glWaterPathStartIdx`/`glWaterPathEndIdx` (:965–966), `gfGuardianClosingInWaitTime` (:1735), `gbShowCredits` (:1849); массивы `gvPutOutTorches_Lamps` и `gvPutOutTorches_Lights` (:1581–1582), локальный `aSound` (:2076). Значения, порядок и размеры сохранены.
- Ссылочные строки AngelScript заменены на C# string, исходные десятичные литералы — float, существующие lifecycle-функции — public override. Статические имена существующих функций выражены через nameof; динамические и отсутствующие имена оставлены строками. Комментарии сохранены.
- В `TimerPutOutTorches` (:1584) после увеличения счётчика добавлена проверка индекса: AngelScript прерывал вызов при выходе за массив; C# возвращается в той же точке, не выполняя последующие эффекты. Штатные три шага не меняются.
- Две ветки стены, вода/прут для ключа, все 59 шагов Guardian и все состояния AlexanderEnding перенесены отдельно.

## Ошибки оригинала и ограничения

- `.map`: `ParticleSystem_47` (ID 3204) имеет пустой File. Старт пишет ошибку создания частиц; источник не исправлен.
- `TimerBlackOut` обращается к `Arealook3`, а в .map есть `Arealook3d`. Имя сохранено.
- `OnStart` регистрирует отсутствующий `CollideAreaGuardianDoor2Checkpoint`. Динамические `EventStep3`, `EventStep5` и другие необъявленные шаги также остаются как в оригинале: фиктивных обработчиков нет.
- `AreaGuardianEffectsFloor_lump_1`, *lump_21_, *lump_26_, `wood_beam_3`, `barrel01_1`, `barrel01_2` отсутствуют в исходной карте и не создаются скриптом. Сброс обращается к ним. Для развилки 28–35 существуют зоны с суффиксами _1/_2; сброс записывает `CaveInUnblockedPath`, а чтение идёт из `CaveInPathUnblocked`. Исходное несовпадение сохранено.
- StopSound/DestroyParticleSystem могут обращаться к уже завершённым или ещё не созданным записям (`slimeps`, `push`, `GuardianScream`, `GuardianEndFearFog`, `SlimeLoopOnPlayer`, `rumblex`). В диагностике это записано отдельно, без подмены логики.
- `SetInsanitySetEnabled` с 2026-10-17 работает (события безумия перенесены (CLAUDE.md «События безумия»): набор `Ch03` включается, `Ch02` выключается; строка `skipped` ниже — из прогона до этого). `InteractConnectPropWithRope`, attached props, `DetachFromStickyArea`, `AutoSave`, `StartCredits` ещё NotPorted. Прямой вызов Sticky/rope callbacks в диагностике проверяет скрипт, а не физическое прикрепление/канат. `ChangePlayerStateToNormal`, PreloadSound/PreloadParticleSystem — пустые API. Grunt уже имеет ИИ, но прохождение всей погони игроком этим тестом не доказано.

## Проверки карты

Все PlayerSimulation: seed 1, фиксированный шаг 1/60, GPU, без -nographics; запуск `RTXMnesia.EditorDev.PlayerSimulation.RunBatch`, карта `maps/main/ch03/25_cell_tunnels.map`. Команды и эффекты прямых call являются диагностикой.

| Отчёт в UnityProj/UMnesiaRTX/Logs | Сценарий | Результат |
|---|---|---|
| `Ch03_Import_25_cell_tunnels.log/.txt` | существующий ConvertLevelBatch, карта ещё до создания класса | ExitCode 0; 27 warnings, 0 errors; отсутствие порта в этом первоначальном импорте ожидаемо |
| `Ch03_Sim_25_start.log/.txt` | обычный OnStart/OnEnter, `watch 30 2; shot start` | ExitCode 0; 5 warnings, 0 errors; пробуждение, движения камеры, голос Александра; снимок просмотрен |
| `Ch03_Sim_25_escape.log/.txt` | scriptDebug; шесть CollideWriggleBarBreak → UseBarOnBricks → CollideBarBendDone → CollideCollapseWall → UseBarOnPipe/CollidePushKey → UseKeyOnDoor; watch 10 после групп; mainarea, w/s по 1 с | ExitCode 0; 6 warnings, 0 errors; дверь разблокирована, прут/ключ и короткий проход проверены |
| `Ch03_Sim_25_water_hammer.log/.txt` | scriptDebug; CollideRopeInteraction → UseBucketOnRope → CollideRopeDown → InteractFilledBucket → UseBucketOnPipe/BucketPourWater → UseHCOnBricks → CollideEscapeCell → CollideAreaCellEvent; watch 10 между группами | ExitCode 0; 8 warnings, 0 errors; имитация прикрепления ведра, водный ключ, молоток, альтернативное обрушение и cell scare |
| `Ch03_Sim_25_guardian_cp.log/.txt` | scriptDebug; StopGuardianCountDown, снять AreaRandSound_; PlayerStartArea_3; CollideStartGuardian; две реальные смерти через CollideAreaGuardianKill с watch 20 после каждой; cave-in1, AfterCaveIn, water tease, EventStep59 | ExitCode 0; 28 warnings, 0 errors; check01 count 1/2, живой игрок в PlayerStartArea_Guardian, здоровье 52; фонари финального шага погашены |
| `Ch03_Audit_25_cell_tunnels.log/.txt` | HplMeshAudit.Run | ExitCode 1: geometryFailures=0, maxVertexError=0; 6 prefab matrix differences; 11 scene failures; 471 resource checks, 0 failures |

Импорт: предупреждения сгруппированы как отсутствующие связи частей rope_beam/corpse, hinge child ID -1 у bucket, неподдерживаемая FogArea, отсутствующая неиспользуемая menu-текстура и первоначальное отсутствие порта. Полный список сохранён в логе.

Аудит не считается PASS. В 10 из 11 missing submesh найден SkinnedMeshRenderer с точным именем; servant_grunt присутствует как SkinnedMeshRenderer «servant_grunt 1» с GUID исходной модели. Проверка сериализованной сцены сохранена в Ch03_RendererReview.json.

Дополнительный Ch03_Sim_25_alexander дошёл до AlexEndState=13 и StartCredits (заглушка, endNum=0 по исходному вызову); реальные титры не реализованы. Полный маршрут основной игры через главу 2 не проверен.


## Итог проверок главы 3

Дата: 2026-10-16. Начальное git status было чистым; чужих изменений не было. Новых механик/API не добавлено. Источники и установленная игра не изменялись, коммит/пуш не выполнялись.

Сверка `powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Chapter ch03 -Map 25_cell_tunnels -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs`: **0 ошибок, 0 предупреждений**. Компиляция подтверждена Unity-прогонами. Независимое сравнение токенов после перечисленных адаптаций C# не нашло различий; все исходные комментарии присутствуют (`Logs/Ch03_TokenReview.txt`, воспроизводимый диагностический Ch03_TokenReview.py).

Проверены 93 используемых API: все имена и сигнатуры существуют. Полная таблица актуальных сигнатур/файлов и ограничений — `Logs/Ch03_API_Inventory.md`. Существенные заглушки и пустые операции:

| API | Фактический статус |
|---|---|
| `AddAttachedPropToProp` | NotPorted / условная реализация |
| `AutoSave` | ✓ (2026-10-17) |
| `ChangePlayerStateToNormal` | пустая операция |
| `DetachFromStickyArea` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `InteractConnectPropWithRope` | ✓ с 2026-10-17: CLAUDE.md «Верёвки, связи, липкие зоны, слизь» |
| `PreloadParticleSystem` | пустая операция |
| `PreloadSound` | пустая операция |
| `RemoveAttachedPropFromProp` | NotPorted / условная реализация |
| `SetInsanitySetEnabled` | ✓ (2026-10-17) |
| `StartCredits` | NotPorted / условная реализация |

Все отчёты далее — внутри `UnityProj/UMnesiaRTX/Logs/`, вне git. PlayerSimulation: `-batchmode -quit`, GPU, `-seed 1`, шаг 1/60; импорт/аудит — `-batchmode -nographics -quit`. Ни один отчёт с WARNING [Script] ERROR не трактуется как чистый скрипт только из-за 0 errors.

| Отчёт | Итог |
|---|---|
| `Ch03_Import_25_cell_tunnels.log/.txt` | ExitCode 0; 27 warnings, 0 errors |
| `Ch03_Sim_25_alexander.log/.txt` | ExitCode 0; 10 warnings, 0 errors |
| `Ch03_Sim_25_escape.log/.txt` | ExitCode 0; 6 warnings, 0 errors |
| `Ch03_Sim_25_guardian_cp.log/.txt` | ExitCode 0; 28 warnings, 0 errors |
| `Ch03_Sim_25_start.log/.txt` | ExitCode 0; 5 warnings, 0 errors |
| `Ch03_Sim_25_water_hammer.log/.txt` | ExitCode 0; 8 warnings, 0 errors |
| `Ch03_Audit_25_cell_tunnels.log/.txt` | **ExitCode 1, общий аудит не PASS**: Models=132, submeshes=245, geometryFailures=0, prefabMatrixDifferences=6, missingCaches=0, maxVertexError=0, maxPrefabMatrixError=1,73205; Scene: instances=1511, submeshMatrices=1855, sharedMeshColliders=1177, failures=11, maxMatrixError=7,6294E-06; Resource regression: checks=471, failures=0 |

Разбор missing submesh: точное имя с существующим рендерером — 10; рендерер с другим именем внутри сущности — 1; полностью отсутствующих рендереров — 0. Это ограничение поиска MeshFilter/имён, а не доказательство независимой проверки всей скелетной геометрии. Подробности: Ch03_RendererReview.json. Разницы матриц prefab и итоговой сцены не смешиваются.

### Точные диагностические fixtures

Базовая команда: `Unity.exe -batchmode -quit -projectPath Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch -amnesiaMap maps/main/ch03/25_cell_tunnels.map -seed 1 -script "<команды>" -logFile <Logs/Ch03_имя.log>`. Флаг scriptDebug включается только там, где указан; глобальные переменные передаются до OnStart. Скриптовые выдачи предметов/снятие таймеров/callbacks — подготовка диагностики, не изменения порта и не доказательство физического решения.

`Ch03_Sim_25_alexander`: scriptDebug=true, globals=`(нет)`.

```text
call StopGuardianCountDown; call RemoveTimer AreaRandSound_; call RemoveTimer TimerBlackOut; call SetPlayerActive true; call StartAlexanderEnding; watch 240 10; call GetLocalVarInt AlexEndState; shot cell_ending
```

### Предупреждения прогонов

- [Script] ERROR: Couldn't create particle system 'ParticleSystem_47' of type ''
- [Script] SetInsanitySetEnabled: skipped, needs insanity events (step 6)
- [Script] InteractConnectPropWithRope: skipped, needs interact connections
- [Script] AutoSave: skipped, needs saving (step 6)
- [Script] ERROR: Could not find sound entity 'slimeps'
- [Script] ERROR: Could not find sound entity 'slime1s'
- [Script] ERROR: Could not find sound entity 'slime2s'
- [Script] ERROR: Could not find sound entity 'slime3s'
- [Script] ERROR: Could not find sound entity 'slime4s'
- [Script] StartCredits: skipped, needs credits (step 6, menus)
- [Script] ERROR: Could not find sound entity 'push'
- [Script] Entity 'AreaGuardianEffectsFloor_lump_1' does not exist!
- [Script] ERROR: Script function 'EventStep3' does not exist in CellTunnels
- [Script] ERROR: Script function 'EventStep5' does not exist in CellTunnels
- [Script] ERROR: Could not find sound entity 'GuardianScream'
- [Script] Could not find any entities with string '*lump_21_'
- [Script] Could not find any entities with string '*lump_26_'
- [Script] Entity 'AreaGuardianKill_lump_28' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_29' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_30' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_31' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_32' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_33' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_34' with type any and subtype any does not exist!
- [Script] Entity 'AreaGuardianKill_lump_35' with type any and subtype any does not exist!
- [Script] Entity 'wood_beam_3' with type Prop and subtype any does not exist!
- [Script] Entity 'barrel01_2' with type Prop and subtype any does not exist!
- [Script] Entity 'barrel01_1' with type Prop and subtype any does not exist!
- [Script] ERROR: Could not find particle system 'GuardianEndFearFog'
- [Script] ERROR: Could not find sound entity 'SlimeLoopOnPlayer'
- [Script] Entity 'wood_beam_3' with type any and subtype any does not exist!
- [Script] Entity 'barrel01_1' with type any and subtype any does not exist!
- [Script] Entity 'barrel01_2' with type any and subtype any does not exist!
- [Script] ERROR: Could not find sound entity 'rumblex'
- [Script] Entity 'Arealook3' does not exist!
- [Script] AddAttachedPropToProp: skipped, needs attached props
- [Script] RemoveAttachedPropFromProp: skipped, needs attached props
- [Script] DetachFromStickyArea: skipped, needs sticky areas

Это сохранённые исходные обращения, недостающие механики или явно указанная диагностическая подготовка. Для одноразовых/уже завершённых звуков и PS отсутствие записи при Stop/Destroy не трактуется как отсутствующий ресурс. Полный порядок сообщений и состояния находятся в соответствующих .txt. Исключений, PortError, takes N arguments и отсутствующих функций порта не обнаружено; исходные отсутствующие callbacks перечислены отдельно.

### Общие регрессии и границы готовности

До изменений: Ch03_Baseline_MapChange.log — **66 PASS**; Ch03_Baseline_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS**. После всех шести портов: Ch03_Regression_MapChange.log — **66 PASS, ExitCode 0**; Ch03_Regression_MainMenu.log — **27 / 50 / 36 / 14 / 14 PASS, ExitCode 0**. Обе Play-регрессии запускались без -quit/-nographics и завершили процесс самостоятельно. Тесты не менялись.

В собственный итог входят шесть новых классов Ch03 с .meta, meta папки, шесть журналов ch03, строка этапа 5 в синхронных AGENTS.md/CLAUDE.md и автоматически добавленные шесть сцен в EditorBuildSettings.asset. Стартовое меню осталось первым. TimeManager/DynamicsManager были чистыми до работы; автоматическое пересохранение Unity убрано после выхода. Планы других глав, старые порты/журналы, api.md, движок, импортёр и тесты не менялись. Игнорируемые ресурсы в git не включены.

Итоговый `git status --short --untracked-files=all` (все перечисленные изменения относятся к этой задаче):

```text
 M AGENTS.md
 M CLAUDE.md
 M UnityProj/UMnesiaRTX/ProjectSettings/EditorBuildSettings.asset
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/CellTunnels.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/InnerSanctum.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/OrbChamber.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureChancelRedux.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/TortureNaveRedux.cs.meta
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs
?? UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch03/ZimmermanVision.cs.meta
?? docs/ScriptPort/ch03/25_cell_tunnels.md
?? docs/ScriptPort/ch03/26_torture_nave_redux.md
?? docs/ScriptPort/ch03/26_zimmerman_vision.md
?? docs/ScriptPort/ch03/27_torture_chancel_redux.md
?? docs/ScriptPort/ch03/28_inner_sanctum.md
?? docs/ScriptPort/ch03/29_orb_chamber.md
```
