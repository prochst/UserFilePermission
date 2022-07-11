# UserFilePermission
Nástroj pro administrátory sdílených souborů v Active Directory doméně, který umožňuje procházet adresář a podadresáře do definované úrovně
a vypsat souborová oprávnění pro zvoleného uživatele. Podle volby vypíše individuální oprávnění nebo i oprávnění skupin, ve kterých je členem.
Nalezaná oprávnění lze exportovat do CSV souboru

- [UserFilePermission](#userFilepermission)
  - [Technologie](#technologie)
  - [Používání](#používání)
    - [Struktura projektu](#struktura-projektu)
    - [Nastavení](#nastavení)
  - [Licence](#licence)
  - [Kontakt](#kontakt)

## Technologie

Projekt je napsán v C# a Net Core 3.1

Projekt je vytvořen ve Visual studiu 2019, 2022

## Používání

### Struktura projektu

-- App.xaml - vstupní bod aplikace

-- MainWindow.xaml - formulář s uživatelským rozhraním

-- CheckPermissions.cs - třída pro výpis oprávnění

-- config.ini - configurační soubor pro přednastavené hodnoty, musí být v adresáři aplikace


### Nastavení

V uživatelském rozhraní se zadávají:

-- Přihlašovací jméno uživatele, pro kterého vypisujeme oprávnění - AD účet

-- Active directory doména s kterou pracujeme 

-- Startovací adresář pro prohledávání

-- Počet úrovní které se budou prohledávat

-- Vypis individuálních oprávnění uživatele nebo i oprávnění skupin, kterých je členem

-- Seznam adresářů oddělených čárkou, kterí se při procházení vynechají


V souboru config.ini lze přenastavit:

-- Active directory doménu

-- Počet úrovní které se budou prohledávat

-- Seznam ignorovaných adresářů


Formát ini souboru

[Default setting]

domain= mydomain.local

levels = 3

directoryexcluded= _osoba, _archiv


## Licence

Distributed under the GNU GENERAL PUBLIC LICENSE. See license.txt for more information.

## Kontakt

Autor: Standa Procházka - prochst.dev@gmmail.com
Projekt: [GitHub](https://github.com/prochst/UserFilePermission)
