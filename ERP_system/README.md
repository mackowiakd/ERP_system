## 🏠 ERP_System – Kompleksowy system do zarządzania budżetem domowym

**ERP_System** to nowoczesna aplikacja webowa przeznaczona do monitorowania wydatków i przychodów, umożliwiająca użytkownikom pełną kontrolę nad ich finansami osobistymi oraz wspólnymi budżetami gospodarstw domowych. Projekt został zrealizowany jako część kursu *Programowanie Komputerów* na Politechnice Śląskiej.

---

### 🏗️ Architektura i Technologie

Projekt opiera się na architekturze warstwowej, co zapewnia separację logiki biznesowej od prezentacji:

*   **`ERP_System.Core`** – Biblioteka klas zawierająca logikę domenową, encje bazy danych, serwisy biznesowe oraz migracje. Wykorzystuje **Entity Framework Core** z dostawcą **SQLite**.
*   **`EPR_System.Web`** – Aplikacja webowa ASP.NET Core wykorzystująca wzorzec **Minimal APIs**. Odpowiada za serwowanie frontendu (HTML/JS) oraz obsługę żądań API. Zawiera również **Background Service** do automatycznego przetwarzania transakcji cyklicznych.
*   **`ERP_System.Tests`** – Projekt testowy wykorzystujący **xUnit**, służący do weryfikacji poprawności logiki biznesowej (transakcje, autoryzacja, zarządzanie domostwem).

**Wykorzystane technologie:**
*   **Backend:** .NET 9.0, C#, ASP.NET Core
*   **Baza danych:** SQLite, Entity Framework Core (EF Core)
*   **Bezpieczeństwo:** `PasswordHasher` (Microsoft.AspNetCore.Identity) do bezpiecznego hashowania haseł, uwierzytelnianie oparte na ciasteczkach (Cookies).
*   **Raportowanie:** QuestPDF (generowanie dokumentów PDF).
*   **Frontend:** Vanilla HTML5, CSS3, JavaScript (ES6+).
*   **Testy:** xUnit, Moq, FluentAssertions.

---

### 📂 Kluczowe Funkcjonalności

*   **Zarządzanie Transakcjami** – Pełna obsługa transakcji (CRUD) z podziałem na przychody i wydatki. 
*   **Automatyzacja (Transakcje Cykliczne)** – System posiada wbudowany proces tła (**Worker Service**), który co godzinę sprawdza i automatycznie generuje zaplanowane transakcje (dzienne, miesięczne itp.).
*   **Interaktywny Kalendarz** – Wizualizacja historii oraz prognoz finansowych.
*   **Zaawansowane Raporty PDF** – Generowanie profesjonalnych zestawień z wykresami słupkowymi i szczegółową listą operacji.
*   **Gospodarstwa Domowe (Households)** – Współdzielenie budżetu z innymi użytkownikami, wspólne śledzenie wydatków domowych.
*   **Bezpieczeństwo i Role** – System ról (Użytkownik, Admin), bezpieczne przechowywanie poświadczeń i sesji.
*   **Konsola Administratora** – Panel do bezpośredniego zarządzania danymi za pomocą zapytań SQL (tylko dla administratorów systemu).

---

### 🛠️ Wymagania i Konfiguracja

*   **.NET 9.0 SDK**
*   System operacyjny: Windows, Linux lub macOS.
*   Opcjonalnie: Narzędzia wiersza poleceń EF Core (`dotnet-ef`).

**Konfiguracja bazy danych:**
Parametry połączenia znajdują się w pliku `ERP_System.Web/appsettings.json`. Domyślnie aplikacja korzysta z lokalnego pliku bazy danych SQLite.

---

## 🚀 Kompilacja, Uruchamianie i Testy

### 1. Sklonuj repozytorium
```bash
git clone https://github.com/mackowiakd/ERP_system/
cd ERP_System
```

### 2. Przygotowanie bazy danych
Jeśli chcesz zaktualizować bazę danych do najnowszej migracji:
```bash
dotnet ef database update --project ERP_System.Core --startup-project ERP_System.Web
```

### 3. Uruchomienie aplikacji
```bash
dotnet run --project ERP_System.Web
```
Aplikacja będzie dostępna pod adresem `http://localhost:5200` (lub innym wskazanym w konsoli).

### 4. Uruchomienie testów
Aby upewnić się, że wszystko działa poprawnie, uruchom zestaw testów jednostkowych:
```bash
dotnet test
```

---

### 🎨 Zasoby i Licencja
Projekt jest realizowany w celach edukacyjnych jako open-source. Wykorzystuje biblioteki na licencjach typu Community/MIT (m.in. QuestPDF, SQLite).
