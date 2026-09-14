# TestProject

Небольшое ASP.NET Core API для постановки задач на формирование пользовательской статистики и получения их статуса. Данные задач хранятся в SQLite, а фоновый `ReportWorker` завершает просроченные задачи.

## Требования

- .NET SDK 10;
- инструмент EF Core CLI. Если он ещё не установлен:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Запуск

Из корня репозитория выполните:

```bash
dotnet restore
dotnet ef database update --project TestProject/TestProject.csproj
dotnet run --project TestProject/TestProject.csproj
```

Команда `database update` создаёт локальный файл `TestDatabase.sqlite` и применяет миграции. Файл базы данных не хранится в Git.

В профиле разработки API доступно по адресу `http://localhost:5095`. OpenAPI-документ доступен только в окружении `Development`.

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

Чтобы создать миграцию после изменения EF Core-модели:

```bash
dotnet ef migrations add <MigrationName> --project TestProject/TestProject.csproj
```

Затем примените её:

```bash
dotnet ef database update --project TestProject/TestProject.csproj
```

Не удаляйте и не изменяйте применённые миграции: последующие изменения схемы оформляйте новой миграцией.

## Тесты

```bash
dotnet test TestProject.Tests/TestProject.Tests.csproj
```

Тесты используют изолированную SQLite in-memory базу и не создают либо не изменяют локальный `TestDatabase.sqlite`.
