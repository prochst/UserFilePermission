# UserFilePermission
Nástroj pro administrátory sdílených souborù v Active Directory doménì, který umožòuje procházet adresáø a podadresáøe do definované úrovnì
a vypsat souborová oprávnìní pro zvoleného uživatele. Podle volby vypíše individuální oprávnìní nebo i oprávnìní skupin, ve kterých je èlenem.
Nalezaná oprávnìní lze exportovat do CSV souboru

- [UserFilePermission](#userFilepermission)
  - [Technologie](#technologie)
  - [Používání](#používání)
    - [Struktura projektu](#struktura-projektu)
    - [Nastavení](#nastavení)
  - [Licence](#licence)
  - [Kontakt](#kontakt)

## Technologie

Projekt je napsán v C# a Net Core 3.1

Projekt je vytvoøen ve Visual studiu 2019, 2022

## Používání

### Struktura projektu

-- App.xaml - vstupní bod aplikace

-- MainWindow.xaml - formuláø s uživatelským rozhraním

-- CheckPermissions.cs - tøída pro výpis oprávnìní

-- config.ini - configuraèní soubor pro pøednastavené hodnoty, musí být v adresáøi aplikace


### Nastavení

V uživatelském rozhraní se zadávají:

-- Pøihlašovací jméno uživatele, pro kterého vypisujeme oprávnìní - AD úèet

-- Active directory doména s kterou pracujeme 

-- Startovací adresáø pro prohledávání

-- Poèet úrovní které se budou prohledávat

-- Vypis individuálních oprávnìní uživatele nebo i oprávnìní skupin, kterých je èlenem

-- Seznam adresáøù oddìlených èárkou, kterí se pøi procházení vynechají


V souboru config.ini lze pøenastavit:

-- Active directory doménu

-- Poèet úrovní které se budou prohledávat

-- Seznam ignorovaných adresáøù


Formát ini souboru

[Default setting]

domain= mydomain.local

levels = 3

directoryexcluded= _osoba, _archiv


## Licence

Distributed under the GNU GENERAL PUBLIC LICENSE. See license.txt for more information.

## Kontakt

Autor: Standa Procházka - prochst@gmmail.com
Projekt: [GitHub](https://github.com/prochst/UserFilePermission)
