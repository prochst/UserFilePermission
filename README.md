# UserFilePermission
N�stroj pro administr�tory sd�len�ch soubor� v Active Directory dom�n�, kter� umo��uje proch�zet adres�� a podadres��e do definovan� �rovn�
a vypsat souborov� opr�vn�n� pro zvolen�ho u�ivatele. Podle volby vyp�e individu�ln� opr�vn�n� nebo i opr�vn�n� skupin, ve kter�ch je �lenem.
Nalezan� opr�vn�n� lze exportovat do CSV souboru

- [UserFilePermission](#userFilepermission)
  - [Technologie](#technologie)
  - [Pou��v�n�](#pou��v�n�)
    - [Struktura projektu](#struktura-projektu)
    - [Nastaven�](#nastaven�)
  - [Licence](#licence)
  - [Kontakt](#kontakt)

## Technologie

Projekt je naps�n v C# a Net Core 3.1

Projekt je vytvo�en ve Visual studiu 2019, 2022

## Pou��v�n�

### Struktura projektu

-- App.xaml - vstupn� bod aplikace

-- MainWindow.xaml - formul�� s u�ivatelsk�m rozhran�m

-- CheckPermissions.cs - t��da pro v�pis opr�vn�n�

-- config.ini - configura�n� soubor pro p�ednastaven� hodnoty, mus� b�t v adres��i aplikace


### Nastaven�

V u�ivatelsk�m rozhran� se zad�vaj�:

-- P�ihla�ovac� jm�no u�ivatele, pro kter�ho vypisujeme opr�vn�n� - AD ��et

-- Active directory dom�na s kterou pracujeme 

-- Startovac� adres�� pro prohled�v�n�

-- Po�et �rovn� kter� se budou prohled�vat

-- Vypis individu�ln�ch opr�vn�n� u�ivatele nebo i opr�vn�n� skupin, kter�ch je �lenem

-- Seznam adres��� odd�len�ch ��rkou, kter� se p�i proch�zen� vynechaj�


V souboru config.ini lze p�enastavit:

-- Active directory dom�nu

-- Po�et �rovn� kter� se budou prohled�vat

-- Seznam ignorovan�ch adres���


Form�t ini souboru

[Default setting]

domain= mydomain.local

levels = 3

directoryexcluded= _osoba, _archiv


## Licence

Distributed under the GNU GENERAL PUBLIC LICENSE. See license.txt for more information.

## Kontakt

Autor: Standa Proch�zka - prochst@gmmail.com
Projekt: [GitHub](https://github.com/prochst/UserFilePermission)
