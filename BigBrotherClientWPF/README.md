# BigBrotherClientWPF

Prosty klient WPF działający w tle, komunikujący się z serwerem TCP.  
Aplikacja umożliwia:

- cykliczne wysyłanie zrzutów ekranu
- utrzymywanie połączenia (ping)
- odbieranie komend (np. blokada ekranu)
- działanie w trayu systemowym

---

##  Funkcjonalności

###  Połączenie z serwerem
Aplikacja łączy się z serwerem TCP:

- IP: `10.10.10.114`
- Port: `6767`

Po połączeniu:
- utrzymuje aktywne połączenie
- nasłuchuje komend

---

### Komunikacja

#### Wysyłane dane:
- `PING` — co 5 sekund (heartbeat)
- `SCREENSHOT|<base64>` — zrzut ekranu co 5 sekund

#### Format wiadomości:
Każda wiadomość:
1. długość (`int`, 4 bajty)
2. payload (UTF-8)

---

### Zrzuty ekranu

- pobierany jest cały ekran (`Screen.PrimaryScreen`)
- zapisywany jako PNG
- kodowany do Base64
- wysyłany do serwera

---

### Obsługiwane komendy

| Komenda        | Opis |
|----------------|------|
| `CMD|LOCK`     | Wyświetla okno blokady |
| `CMD|UNLOCK`   | Zamka okno blokady |

---

### Tryb działania (Tray)

Po uruchomieniu aplikacja:

- ukrywa główne okno
- działa w tle
- pojawia się w trayu systemowym

#### Opcje w trayu:

- **Status** – informacja o działaniu
- **Quit** – zamyka aplikację

---


### Wymagania

- .NET (WPF)
- Windows

### Kroki

```bash
git clone <repo>
cd BigBrotherClientWPF