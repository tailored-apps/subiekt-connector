# 🏪 Subiekt 123 — Demo App

Blazor Server demo z wpiętym **Subiekt Connector SDK**.  
Pokazuje kompletny przepływ OAuth 2.0 PKCE + przeglądanie klientów, dokumentów i produktów.

---

## Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Konto w [portalu deweloperskim InsERT](https://developers.insert.com.pl)
- Aktywna subskrypcja API Subiekt 123

---

## Konfiguracja

Edytuj `appsettings.json`:

```json
{
  "Subiekt": {
    "ClientId": "twój-client-id",
    "ClientSecret": "twój-client-secret",
    "SubscriptionKey": "klucz-subskrypcji",
    "RedirectUri": "https://localhost:5002/hook/callback"
  }
}
```

> ⚠️ Nigdy nie commituj `appsettings.json` z prawdziwymi kluczami.  
> Użyj `appsettings.Development.json` (jest w `.gitignore`) lub zmiennych środowiskowych.

### Jak uzyskać credentials

1. Wejdź na [developers.insert.com.pl](https://developers.insert.com.pl)
2. **Zarejestruj aplikację** → podaj nazwę, opis, adres powrotu  
   Adres powrotu (redirect URI): `https://localhost:5002/hook/callback`
3. Skopiuj `ClientId` i `ClientSecret`
4. Kliknij **Subskrybuj** przy API Subiekt 123 → skopiuj `SubscriptionKey`

---

## Uruchomienie

```bash
cd demo/Subiekt.Demo
dotnet run
```

Otwórz: **`https://localhost:5002`**

> Przeglądarka może ostrzec o certyfikacie — kliknij "Zaawansowane → Kontynuuj".  
> Aby zainstalować certyfikat deweloperski: `dotnet dev-certs https --trust`

---

## Przepływ logowania

```
1. Otwórz https://localhost:5002/auth
2. Kliknij "🔐 Zaloguj się przez InsERT"
3. Zostaniesz przekierowany na stronę logowania InsERT
4. Zaloguj się na swoje Konto InsERT
5. Wyraź zgodę na dostęp do danych Subiekt 123
6. Aplikacja automatycznie odbierze token i wróci na stronę główną
7. Możesz teraz przeglądać klientów, dokumenty i produkty
```

---

## Strony aplikacji

| Adres | Opis |
|-------|------|
| `/` | Lista klientów z filtrowaniem i paginacją |
| `/dokumenty` | Lista dokumentów (faktury, paragony) |
| `/produkty` | Lista produktów/towarów |
| `/auth` | Status autoryzacji, logowanie/wylogowanie |

---

## Zmienne środowiskowe (alternatywa dla appsettings)

```bash
export Subiekt__ClientId="twój-client-id"
export Subiekt__ClientSecret="twój-client-secret"
export Subiekt__SubscriptionKey="klucz"
export Subiekt__RedirectUri="https://localhost:5002/hook/callback"

dotnet run
```

---

## Uwagi

- Token jest przechowywany **w pamięci** — po restarcie aplikacji trzeba zalogować się ponownie
- Token ważny **60 minut**, refresh token **90 dni** — aplikacja odświeża automatycznie
- Do produkcji: zamień `InMemoryTokenStore` na implementację z bazą danych lub Redis
