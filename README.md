# TestProject

Небольшое ASP.NET Core API для постановки задач на формирование пользовательской статистики и получения их статуса. Данные задач хранятся в SQLite, а фоновый `ReportWorker` завершает просроченные задачи.

## Требования

- .NET SDK 10.

## Запуск

Откройте `TestProject.sln` в Rider или Visual Studio и запустите основной проект. При первом запуске приложение создаст локальный файл `TestDatabase.sqlite` и применит миграции. Файл базы данных не хранится в Git.

Для запуска из терминала выполните из корня репозитория:

```bash
dotnet run --project TestProject/TestProject.csproj
```

API доступно по адресу `http://localhost:5095`. OpenAPI-документ доступен в окружении `Development`.

## Настройки

Настройки находятся в `TestProject/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=TestDatabase.sqlite"
  },
  "ReportProcessing": {
    "DurationMilliseconds": 60000
  },
  "ReportWorker": {
    "PollingInterval": "00:00:01"
  }
}
```

- `DefaultConnection` — строка подключения SQLite;
- `DurationMilliseconds` — длительность выполнения отчёта в миллисекундах;
- `PollingInterval` — период опроса задач фоновым воркером в формате `TimeSpan`.

Значения `DurationMilliseconds` и `PollingInterval` должны быть больше нуля; приложение проверяет это при запуске.

## API

### Постановка задачи статистики

`POST /report/user_statistics`

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "from": "2026-01-01",
  "to": "2026-01-31"
}
```

Поля `userId`, `from` и `to` обязательны. Дата начала периода не может быть позже даты окончания.

Ответ — идентификатор созданной задачи:

```json
"1a98b57d-e090-4d18-8654-678e463b73e8"
```

### Получение состояния задачи

`GET /report/info?query={reportJobId}`

До завершения задачи:

```json
{
  "query": "1a98b57d-e090-4d18-8654-678e463b73e8",
  "percent": 50,
  "result": null
}
```

После завершения:

```json
{
  "query": "1a98b57d-e090-4d18-8654-678e463b73e8",
  "percent": 100,
  "result": {
    "user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "count_sign_in": 10
  }
}
```

Прогресс вычисляется по времени создания задачи и `ReportProcessing:DurationMilliseconds`. При первом обращении после истечения заданного времени задача также актуализируется в базе, поэтому завершённая задача всегда возвращается с `percent: 100` и результатом.

## База данных и миграции

Текущая миграция создаёт таблицу `ReportJobs` со статусом задачи и необязательным полем `CountSignIn`. Статус сохраняется в базе текстом; для `CountSignIn` действует ограничение: значение не может быть отрицательным.

### Ручное применение миграций

Для работы с миграциями вручную установите EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

Затем из корня репозитория выполните:

```bash
dotnet ef database update --project TestProject/TestProject.csproj
```

Команда использует строку подключения из `appsettings.json`, при необходимости создаёт базу и применяет только отсутствующие миграции.

## Тесты

```bash
dotnet test TestProject.Tests/TestProject.Tests.csproj
```

Тесты используют изолированную SQLite in-memory базу и не создают либо не изменяют локальный `TestDatabase.sqlite`.
