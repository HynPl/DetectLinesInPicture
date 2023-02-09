![priklad](https://github.com/HynPl/DetectLinesInPicture/blob/main/images/vizualizace.jpg?raw=true)

# DetectLinesInPicture
## O Projektu
Plugin do Grasshopperu určený k detekci čar z obrázku.

## Jak funguje detekce
Samotná detekce hran uvnitř komponenty probíhá tak, že se nejprve předběžně prohledá obrázek, zjistí se, kde je něco bílé. V druhém kroku komponenta vyšle z předběžných bílých míst „pac-many“ jdoucí po bílém. Průběžně se ukládá do seznamu kudyma šli. Jako když jde člověk po čerstvě napadlém sněhu, jdou vidět jeho stopy. V třetím kroku dojde k napojování seznamů cest. Na závěr dojde ke zjednodušení výsledku a exportu na křivky (Polyline).

## Další součásti pluginu
- Plugin obsahuje i jednoduchou úpravu obrázků k tvorbě "masky" bitmapy, potřebou k detekci čar.
- Vykreslení bitmapy
- Získání rozměrů obrázku a získání barvy bitmapy

## Naprogramováno v
- C# 
- .NET Framework 4.8
- Visual Studio 2022
