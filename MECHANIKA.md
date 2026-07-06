# Mechanika Kodu C# w Projekcie Tycoon

Ten dokument zawiera szczegółowy opis architektury i mechaniki działania kodu C# w omawianym projekcie Unity (typ gry: Tycoon/Ekonomia). System opiera się na luźnym powiązaniu komponentów (decoupling) przy użyciu `EventBus`, zarządzaniu stanem gry przez zbiór Managerów (wzorzec Singleton) oraz definiowaniu danych za pomocą `ScriptableObject`.

## 1. Architektura i Komunikacja

### EventBus (System Zdarzeń)
Kluczowym elementem architektury, zapewniającym brak bezpośrednich zależności (tzw. "spaghetti code") pomiędzy różnymi systemami gry (np. Ekonomia nie musi znać UI, Budynki nie muszą bezpośrednio wywoływać metod w Statystykach), jest wzorzec **EventBus** (`Assets/Scripts/Core/EventBus.cs`).
*   **Działanie:** Systemy mogą publikować zdarzenia (Publish), używając struktur zdefiniowanych w `GameEvents.cs` (np. `TickEvent`, `ProductionCompletedEvent`, `BuildingPlacedEvent`). Inne systemy rejestrują chęć nasłuchiwania na konkretny typ zdarzenia (Subscribe).
*   **Cykl życia:** Subskrypcje są rejestrowane w metodzie `OnEnable` i **muszą** być wyrejestrowane w `OnDisable`, aby zapobiec wyciekom pamięci.
*   **Bezpieczeństwo:** Podczas publikacji zdarzenia system tworzy kopię listy subskrybentów, co chroni przed błędami w przypadku, gdy handler zdarzenia spróbuje dodać lub usunąć subskrypcję w trakcie iteracji.

### Managerowie (Singletons)
Główne logiki systemowe podzielono na osobne klasy z wzorcem Singleton, by ułatwić globalny dostęp:
*   `GameManager`: Inicjalizuje systemy i trzyma ogólny stan gry (`GameState.Running`, `Paused`).
*   `TickManager`: Służy jako silnik napędowy dla wszystkiego, co wymaga czasu.
*   `GameCalendar`: Tłumaczy upływ czasu na godziny, dni, miesiące i pobiera koszty utrzymania.
*   `EconomyManager`: Zarządza portfelem gracza, cenami oraz oblicza popyt.
*   `PlayerManager`: Przechowuje dodatkowe dane gracza, ale fundusze oddelegowuje głównie do weryfikacji w `EconomyManager`.
*   `StatisticsManager`: Zapisuje wszelkie dane historyczne gry (ile wyprodukowano, zarobiono).
*   `GridManager`: Zarządza matematyką i danymi 2D, siatką dla budynków.
*   `UIManager`: Odpowiada za uaktualnianie danych w interfejsie w oparciu o informacje odbierane z EventBusa.

### Dane Zewnętrzne (ScriptableObjects)
Zamiast "twardo" kodować budynki lub produkty w kodzie, zastosowano `ScriptableObject`, które tworzą bazę danych (`ProductDatabase`, `RecipeDatabase`, `BuildingDatabase`):
*   `ProductData`: Zawiera m.in. `id`, `basePrice`, `baseDemand` i wzory obliczające krzywą popytu.
*   `RecipeData`: Określa co budynek konsumuje, co produkuje, w jakich proporcjach oraz ile zajmuje to ticków.
*   `BuildingData`: Obejmuje gabaryty budynku, koszt postawienia/wyburzenia/utrzymania i powiązaną recepturę produkcyjną.

---

## 2. Upływ Czasu (Czas Gry)

Czas w grze jest odseparowany od `Time.deltaTime` tradycyjnie używanego w `Update` poszczególnych skryptów, stosując architekturę opartą na "Tyknięciach" (Ticks).

### TickManager (`Assets/Scripts/Core/TickManager.cs`)
*   To "Serce" gry. Posiada konfigurowalny `_tickInterval` (ile sekund rzeczywistych to 1 Tick gry).
*   Może przyspieszać czas zmieniając zmienną `_timeScale` lub być pauzowany.
*   Co każdy interwał publikuje `TickEvent`, w którym zawiera numer obecnego ticka. Wszystkie inne systemy (np. produkcja w budynkach, gospodarka, UI) uaktualniają się **tylko** reagując na to wydarzenie.

### GameCalendar (`Assets/Scripts/Core/GameCalendar.cs`)
*   Odbiera `TickEvent`. Wprowadza zasady: 1 Tick = 1 Godzina, 24 Ticki = 1 Dzień, 30 Dni = 1 Miesiąc.
*   Tłumaczy Ticki na bardziej przyjazny dla człowieka kalendarz (Dzień 1, 12:00 itp.).
*   Gdy mija miesiąc, publikuje `MonthPassedEvent` oraz obciąża portfel gracza, odpytując każdy budynek na scenie o koszty utrzymania. Publikuje powiązane z tym zdarzenie `MaintenancePaidEvent`.

---

## 3. System Budowania (Siatka 2D)

System rozmieszczania elementów w grze został podzielony na kilka niezależnych komponentów:

### GridManager (`Assets/Scripts/Grid/GridManager.cs`)
Odpowiada za całą matematykę układu dwuwymiarowego, zajmowane kafelki, zamianę koordynatów World Space na siatkę gry (i odwrotnie), oraz weryfikowanie kolizji.

### BuildingPlacer (`Assets/Scripts/Buildings/BuildingPlacer.cs`)
Obsługuje bezpośrednio proces dodawania nowej struktury:
1.  Rozpoczyna od `StartPlacing(BuildingData)`.
2.  Aktualizuje widok "ducha" (półprzezroczysty, zielony, gdy można budować; czerwony, gdy nie). Z użyciem nowego `Input System` i śledzeniem `Mouse.current.position`.
3.  Po lewym kliknięciu następuje walidacja (czy zajęte przez `GridManager`? czy gracza na to stać u `PlayerManager`?).
4.  W przypadku sukcesu: Pobiera gotówkę, zajmuje miejsce w `GridManager`, wywołuje `Instantiate` na prefabrykacie przypisanym do definicji budynku, ustawia wartości (m.in. przypisuje recepturę), oraz emituje `BuildingPlacedEvent`.

### BuildingSelector (`Assets/Scripts/Buildings/BuildingSelector.cs`)
Służy do interakcji po zbudowaniu. Rzuca `Raycast2D`. Gdy trafi na budynek, podświetla go, nadając oryginalnemu `SpriteRenderer` kolor selekcji i publikuje zdarzenie `BuildingSelectedEvent`. Odbiorcą zazwyczaj jest `UIManager`, który ma wtedy zaktualizować okno własności budynku.

### Klasa Bazowa `Building` i Pochodne
*   `Building.cs`: Klasa zawierająca podstawowe właściwości (Pozycja na planszy, rozmiar `footprint`, poziom ulepszeń, koszt wybudowania i koszt miesięcznego utrzymania).
*   `ProductionBuilding.cs`: Główna implementacja specjalistyczna dziedzicząca po `Building`. To tu dzieje się "magia" Tycoon'a. Posiada ona własne zmienne jak wskaźniki podłączeń do innych budynków, aktualnie przypisaną recepturę (`RecipeData`), swój wewnętrzny magazynek produktów, flagę optymalizacji `_isDirty` i funkcję Auto-Sprzedaży.

---

## 4. Ekonomia i Produkcja

Gra polega na przetwarzaniu surowców lub ich sprzedawaniu, a główną trudnością jest osiąganie marży przy zachodzącym popycie i zmiennych cenach.

### ProductionBuilding - Cykl Życia (`Assets/Scripts/Buildings/ProductionBuilding.cs`)
Budynek produkcyjny nasłuchuje `TickEvent`. Kiedy tick nadejdzie:
1.  Sprawdza, czy ma przypisaną recepturę (`RecipeData`).
2.  Sprawdza magazyn, by zobaczyć, czy posiada wymagane `RecipeIngredient`s (np. czy piekarnia ma odpowiednio dużo mąki z innych budynków).
3.  Pobiera surowce i inkrementuje progres produkcji (`_productionProgress`).
4.  Kiedy progres >= `productionTimeTicks`, proces dobiega końca. Wywoływane jest `CompleteProductionCycle()`.
5.  Towary są dodawane do magazynu własnego budynku, a sam budynek publikuje `ProductionCompletedEvent`.
6.  Jeśli Auto-Sprzedaż jest włączona, obiekt próbuje opróżnić magazyn ze świeżego gotowego dobra używając procedury `SellAll()`.

**Optymalizacja Dirty Flag:** Aby nie procesować każdego budynku produkcyjnego, który nie ma surowców, `ProductionBuilding` używa flagi `_isDirty`. Jeśli nie można wyprodukować przedmiotu, obiekt "zasypia", póki inne systemy (np. logistyka/łączność z nowym źródłem) nie przebudzą go funkcją `MarkDirty()`.

### Popyt, Ceny i EconomyManager (`Assets/Scripts/Economy/EconomyManager.cs`)
*   `EconomyManager` zajmuje się bilansem konta, lecz przede wszystkim kalkuluje **Popyt** dla produktów, wykorzystując właściwości zdefiniowane w plikach `ProductData`.
*   System bazuje na własnym rejestrze cen gracza (`_playerPrices`). Gracz może modyfikować ceny sprzedaży z poziomu panelu w UI.
*   `EconomyManager` co każdy Tick przegląda wszystkie zarejestrowane artykuły z `ProductDatabase` i przelicza na nowo popyt dla każdego, bazując na prostym wzorze elastyczności (wymaganie na rynku względem narzuconej kwoty).
*   W procedurze `SellAll()` budynek limituje, jak bardzo opróżni magazyn. Sprawdza limit u `EconomyManager`, czy popyt na rynku ogóle pozwoli sprzedać w jednej turze pełen zasób, który posiada. Przychód uderza ostatecznie do konta zarządzanego przez `PlayerManager`. Zdarzenie `SaleCompletedEvent` powiadamia UI i statystyki.

---

## 5. UI (Interfejs Użytkownika)

Do obsługi interfejsu (i separacji logiki od widoków) jest używany głównie nowy standard `UI Toolkit`.

### UIManager (`Assets/Scripts/UI/UIManager.cs`)
Klasa `UIManager` wiąże logikę z plikami `.uxml` / `.uss`:
*   Nasłuchuje wydarzeń z `EventBus` (`BuildingSelectedEvent`, `BuildingDeselectedEvent`, `TickEvent`, `ProductionCompletedEvent`, `TimeUpdatedEvent`).
*   Wyświetla dane w prawym dolnym logu wydarzeń.
*   Kiedy otrzyma `BuildingSelectedEvent`, buduje/odświeża widok (panel inspekcji/właściwości). Oblicza na bieżąco złożone wartości analityczne (takie jak w prawdziwych Tycoonach), np.:
    *   `Avg Throughput`: Średnia produkcji na Tick bazująca na szybkości i output'cie danej maszyny.
    *   `Cost per unit`: Szacowany koszt pojedynczego wyprodukowanego przedmiotu (Biorąc pod uwagę zapotrzebowania, podstawową kwotę z bazy i koszta stałe budynku).
    *   `Marża`: (Cena Sprzedaży - Koszt wytworzenia jednostki) / Cena sprzedaży * 100.
    *   `Payback`: W jaki czas inwestycja postawienia tego budynku zwróci mu się na czysto w odniesieniu do jego zarobków Dziennych/Miesięcznych.
*   Zapewnia również wsparcie dla uniemożliwienia "przeklikiwania" przez okienka do siatki gry: funkcja `IsPointerOverUI()` wykorzystywana przez narzędzia do budowania, by nie zaznaczać i nie stawiać maszyn, jeśli gracz używa myszki w obrębie panelu UI.

---

## 6. Statystyki

Gra nie wykorzystuje pojedynczych, skomplikowanych liczników rozsianych po klasach. Zamiast tego zaimplementowany jest oddzielny `StatisticsManager` (`Assets/Scripts/Statistics/StatisticsManager.cs`).
*   Śledzi wszystko kompletnie niezależnie (ponieważ po prostu reaguje na wydarzenia rzucane po zrealizowanych akcjach na `EventBus`).
*   Trzyma skumulowane i niewygasające liczniki: np. ilość sprzedanych/wyprodukowanych przedmiotów, ogólną dotychczasową sumę przychodu, wydatków i ilość Ticks od startu programu.
*   Wykorzystuje to proste słowniki `Dictionary<string, float>`, gdzie identyfikatorem jest ID konkretnego produktu, używając metody pomocniczej `AddToCounter` lub `GetCounter` do manipulowania zbiorem.
