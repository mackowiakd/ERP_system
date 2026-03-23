ERP_System – Kompleksowy system do zarządzania budżetem firmy

ERP_System to aplikacja webowa przeznaczona do monitorowania wydatków i przychodów w firmie. Projekt został zrealizowany jako część kursu Tworzenie Aplikacji Bazodanowych na Politechnice Śląskiej.

---

Kluczowe Funkcjonalności:

Logowanie/rejestracja	                    - Uwierzytelnianie użytkowników w systemie z wykorzystaniem bezpiecznego hashowania haseł oraz zarządzanie sesją
Tworzenie profilu nowej Firmy	            - Zakładanie nowego niezależnego bytu firmowego w systemie i generowanie unikalnego kodu zaproszenia (JoinCode) dla nowych pracowników.
Zarządzanie listą pracowników	            - Przeglądanie listy zatrudnionych osób, przypisywanie im odpowiednich ról w systemie (np. Księgowy) oraz możliwość ich usunięcia.
Dodawanie i modyfikacja Kontrahentów	    - Tworzenie i aktualizacja wewnętrznej bazy klientów oraz dostawców (m.in. Nazwa, NIP, Adres) na potrzeby wystawiania faktur
Dodawanie Faktur kosztowych/sprzedażowych   - Wprowadzanie do ewidencji dokumentów księgowych z określeniem kwoty brutto, kontrahenta oraz terminu wymagalności płatności.
Ewidencja wpłat i rozliczanie faktur        - Rejestrowanie rzeczywistych operacji finansowych (wpływów/wypływów) oraz możliwość powiązania ich z konkretnymi fakturami (rozliczenia częściowe i całkowite).
Zarządzanie operacjami cyklicznymi	        - Definiowanie stałych, powtarzalnych zobowiązań (np. leasingi, abonamenty), które system będzie generował automatycznie w oparciu o harmonogram.
Generowanie raportów finansowych (PDF)	    - Tworzenie zaawansowanych analitycznych zestawień w formacie PDF (Zyski/Straty, Wiekowanie Rozrachunków) z wykorzystaniem agregacji bazy danych (QuestPDF).
Bezpośrednie odpytywanie bazy (Konsola SQL)	- Dostępny tylko dla Administratora Systemu moduł serwisowy, umożliwiający wprowadzanie bezpośrednich zapytań SQL na bazie SQLite w celu diagnostyki.

---

Wymagania i Konfiguracja

   WYMAGANY: .NET 9.0 SDK
   System operacyjny kompatybilny z .NET 9.0 SDK.
   Opcjonalnie: Narzędzia wiersza poleceń EF Core (`dotnet-ef`).

Konfiguracja bazy danych:
Parametry połączenia znajdują się w pliku `ERP_System.Web/appsettings.json`. Domyślnie aplikacja korzysta z lokalnego pliku bazy danych SQLite.


Kompilacja, Uruchamianie i Testy
1. Sklonuj repozytorium

w cmd / terminalu:
git clone https://github.com/mackowiakd/ERP_system/
cd ERP_System

Lub pobierz plik .zip i go rozpakuj

2. Przygotowanie bazy danych (potrzebne tylko podczas robienia zmian w bazie danych)
Jeśli chcesz zaktualizować bazę danych do najnowszej migracji:

dotnet ef database update --project ERP_System.Core --startup-project ERP_System.Web



3. Uruchomienie aplikacji

Linux / MacOS / cmd na win 11/10:

dotnet run --project ERP_System.Web

Aplikacja będzie dostępna pod adresem `http://localhost:5200` (lub innym wskazanym w konsoli).

Visual Studio na Windows:

Otworzyć plik ERP_System.sln za pomocą preferowanej wersji Visual Studio (testowano na VS 2023 / 2026)

Należy wybrać projekt ERP_System.Web do uruchomienia 

Kliknąć zieloną strzałkę "Uruchom"

Aplikacja będzie dostępna pod adresem `http://localhost:5000` (lub innym wskazanym w konsoli).

4. Uruchomienie testów
Aby upewnić się, że wszystko działa poprawnie, uruchom zestaw testów jednostkowych:

W terminalu / cmd:
dotnet test

Graficznie w Visual Studio:
Należy wybrać projekt ERP_System.Web i go uruchomić


---

Architektura i Technologie


`ERP_System.Core` – Biblioteka klas zawierająca logikę domenową, encje bazy danych, serwisy biznesowe oraz migracje. Wykorzystuje Entity Framework Core z dostawcą SQLite.
`EPR_System.Web`  – Aplikacja webowa ASP.NET Core wykorzystująca wzorzec Minimal APIs. Odpowiada za serwowanie frontendu (HTML/JS) oraz obsługę żądań API. Zawiera również Background Service do automatycznego przetwarzania transakcji cyklicznych.
`ERP_System.Tests` – Projekt testowy wykorzystujący xUnit, służący do weryfikacji poprawności logiki biznesowej (transakcje, autoryzacja, zarządzanie domostwem).

Wykorzystane technologie:
Backend: .NET 9.0, C#, ASP.NET Core
Baza danych: SQLite, Entity Framework Core (EF Core)
Bezpieczeństwo: `PasswordHasher` (Microsoft.AspNetCore.Identity) do bezpiecznego hashowania haseł, uwierzytelnianie oparte na ciasteczkach (Cookies).
Raportowanie: QuestPDF (generowanie dokumentów PDF).
Frontend: Vanilla HTML5, CSS3, JavaScript (ES6+).
Testy: xUnit, Moq, FluentAssertions.

---

Zasoby i Licencja
Projekt jest realizowany w celach edukacyjnych jako open-source. Wykorzystuje biblioteki na licencjach typu Community/MIT (m.in. QuestPDF, SQLite).
