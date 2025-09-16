# 🌳 СТРУКТУРА ПРОЕКТА "Антиплагиат-Помощник"

## 📁 **КОРНЕВАЯ ДИРЕКТОРИЯ**
```
antiplagiat-detector/
├── 📄 Plagiat.sln                    # Решение Visual Studio
├── 📄 README.md                      # Основная документация
├── 📄 simple_test.txt                # Тестовый файл
├── 📄 test_document.txt              # Тестовый документ
│
├── 📁 packages/                      # NuGet пакеты
│   ├── 📁 DocumentFormat.OpenXml.2.20.0/    # Работа с DOCX
│   ├── 📁 EntityFramework.6.5.1/            # ORM для БД
│   ├── 📁 HandyControls.3.6.0/              # UI компоненты
│   ├── 📁 HandyControls.Lang.ru.3.6.0/      # Русская локализация
│   ├── 📁 HtmlAgilityPack.1.11.54/          # Парсинг HTML
│   ├── 📁 iTextSharp.5.5.13.3/              # Работа с PDF
│   ├── 📁 Microsoft.Extensions.Http.6.0.0/  # HTTP клиент
│   └── 📁 Newtonsoft.Json.13.0.3/           # JSON сериализация
│

├── 📁 Plagiat/                       # ОСНОВНОЙ ПРОЕКТ
│   ├── 📄 Plagiat.csproj             # Файл проекта
│   ├── 📄 App.config                 # Конфигурация приложения
│   ├── 📄 App.xaml                   # Главный XAML
│   ├── 📄 App.xaml.cs                # Код главного приложения
│   ├── 📄 MainWindow.xaml            # Главное окно (XAML)
│   ├── 📄 MainWindow.xaml.cs         # Главное окно (код)
│   ├── 📄 packages.config            # Конфигурация пакетов
│   │
│   ├── 📁 bin/                       # Скомпилированные файлы
│   │   ├── 📁 Debug/                 # Debug сборка
│   │   └── 📁 Release/               # Release сборка
│   │
│   ├── 📁 obj/                       # Временные файлы сборки
│   │   ├── 📁 Debug/
│   │   └── 📁 Release/
│   │
│   ├── 📁 Properties/                # Свойства проекта
│   │   ├── 📄 AssemblyInfo.cs        # Информация о сборке
│   │   ├── 📄 Resources.Designer.cs  # Ресурсы (код)
│   │   ├── 📄 Resources.resx         # Ресурсы (данные)
│   │   ├── 📄 Settings.Designer.cs   # Настройки (код)
│   │   └── 📄 Settings.settings      # Настройки (данные)
│   │
│   ├── 📁 Models/                    # МОДЕЛИ ДАННЫХ
│   │   ├── 📄 Citation.cs            # Модель цитаты
│   │   ├── 📄 Document.cs            # Модель документа
│   │   ├── 📄 PlagiarismResult.cs    # Модель результата плагиата
│   │   ├── 📄 Project.cs             # Модель проекта
│   │   └── 📄 Source.cs              # Модель источника
│   │
│   ├── 📁 Data/                      # РАБОТА С БАЗОЙ ДАННЫХ
│   │   └── 📄 PlagiatContext.cs      # Entity Framework контекст
│   │
│   ├── 📁 Services/                  # БИЗНЕС-ЛОГИКА
│   │   ├── 📄 AntiPlagiatService.cs  # Проверка плагиата (Advego API)
│   │   ├── 📄 BibliographyService.cs # Генерация библиографии
│   │   ├── 📄 CitationService.cs     # Работа с цитатами
│   │   ├── 📄 DatabaseInitializer.cs # Инициализация БД
│   │   ├── 📄 DataService.cs         # CRUD операции
│   │   ├── 📄 DocumentService.cs     # Импорт/экспорт документов
│   │   └── 📄 OpenRouterService.cs   # AI перефразирование
│   │
│   ├── 📁 Views/                     # ПОЛЬЗОВАТЕЛЬСКИЙ ИНТЕРФЕЙС
│   │   ├── 📄 MainWindow.xaml        # Главное окно (дубликат)
│   │   ├── 📄 MainWindow.xaml.cs     # Главное окно (дубликат)
│   │   ├── 📄 SimpleParaphraseDialog.xaml      # Диалог перефразирования
│   │   └── 📄 SimpleParaphraseDialog.xaml.cs   # Диалог перефразирования (код)
│   │
│   ├── 📁 Converters/                # КОНВЕРТЕРЫ ДАННЫХ
│   │   └── 📄 PlagiarismLevelToColorConverter.cs # Конвертер цветов
│   │
│   └── 📁 TestDocuments/             # ТЕСТОВЫЕ ДОКУМЕНТЫ
│       ├── 📄 01_original_text.txt           # Оригинальный текст
│       ├── 📄 02_high_plagiarism.txt         # Высокий плагиат
│       ├── 📄 03_medium_plagiarism.txt       # Средний плагиат
│       ├── 📄 04_low_plagiarism.txt          # Низкий плагиат
│       ├── 📄 05_with_citations.txt          # С цитатами
│       ├── 📄 06_scientific_paper.txt        # Научная статья
│       ├── 📄 07_paraphrase_test.txt         # Тест перефразирования
│       ├── 📄 08_bibliography_test.txt       # Тест библиографии
│       ├── 📄 09_mixed_content.txt           # Смешанный контент
│       ├── 📄 10_test_document.rtf           # RTF документ
│       ├── 📄 11_simple_test.html            # HTML документ
│       ├── 📄 README_TestDocuments.md        # Описание тестов
│       └── 📄 TESTING_GUIDE.md               # Руководство по тестированию
│
└── 📁 ДОКУМЕНТАЦИЯ/                  # ДОКУМЕНТАЦИЯ ПРОЕКТА
    ├── 📄 ADVEGO_API_INTEGRATION.md          # Интеграция с Advego API
    ├── 📄 ADVEGO_API_STATUS.md               # Статус Advego API
    ├── 📄 API_INTEGRATION_STATUS.md          # Статус интеграции API
    ├── 📄 CONSOLE_DEBUG_INSTRUCTIONS.md      # Инструкции по отладке
    ├── 📄 CONTENT_DISPLAY_FIX.md             # Исправление отображения
    ├── 📄 CONTENT_LOSS_DEBUG.md              # Отладка потери контента
    ├── 📄 DATABASE_ERROR_FIX.md              # Исправление ошибок БД
    ├── 📄 DATABASE_PERSISTENCE_FIX.md        # Исправление сохранения БД
    ├── 📄 DATABASE_SETUP.md                  # Настройка базы данных
    ├── 📄 DEBUG_EMPTY_CONTENT.md             # Отладка пустого контента
    ├── 📄 EMPTY_CONTENT_DEBUG.md             # Отладка пустого контента
    ├── 📄 FINAL_VALIDATION_FIX.md            # Финальные исправления
    ├── 📄 FOREIGN_KEY_FIX.md                 # Исправление внешних ключей
    ├── 📄 LATEST_FIX.md                      # Последние исправления
    ├── 📄 PARAPHRASE_DIALOG_DESIGN_UPDATE.md # Обновление дизайна диалога
    ├── 📄 PARAPHRASING_SYSTEM_EXPLANATION.md # Объяснение системы перефразирования
    ├── 📄 RICH_TEXTBOX_FIX.md                # Исправление RichTextBox
    ├── 📄 STARTUP_AND_TEXT_DISPLAY_FIX.md    # Исправление запуска и отображения
    ├── 📄 SYNTAX_ERROR_FIX.md                # Исправление синтаксических ошибок
    ├── 📄 TEST_IMPORT_FIX.md                 # Исправление импорта тестов
    ├── 📄 TEXT_AND_PLAGIARISM_DISPLAY_FIX.md # Исправление отображения текста и плагиата
    ├── 📄 TEXT_RU_API_FIX.md                 # Исправление Text.ru API
    ├── 📄 UPDATE_INSTRUCTIONS.md             # Инструкции по обновлению
    └── 📄 VALIDATION_ERROR_FIX.md            # Исправление ошибок валидации
```

## 🗃️ **СКРИПТЫ И КОНФИГУРАЦИЯ**
```
📁 СКРИПТЫ/
├── 📄 check_database.sql             # Проверка базы данных
├── 📄 database_migration_001.sql     # Миграция БД
├── 📄 database_schema.sql            # Схема базы данных
├── 📄 delete_database.sql            # Удаление БД
├── 📄 force_reset_database.bat       # Принудительный сброс БД
└── 📄 reset_database.bat             # Сброс БД
```

## 🏗️ **АРХИТЕКТУРА ПРОЕКТА**

### **📱 ПРЕЗЕНТАЦИОННЫЙ СЛОЙ (Views)**
- `MainWindow.xaml/cs` - Главное окно приложения
- `SimpleParaphraseDialog.xaml/cs` - Диалог выбора вариантов перефразирования

### **🧠 БИЗНЕС-ЛОГИКА (Services)**
- `AntiPlagiatService.cs` - Проверка плагиата через Advego API
- `OpenRouterService.cs` - AI перефразирование через OpenRouter
- `CitationService.cs` - Обнаружение и обработка цитат
- `BibliographyService.cs` - Генерация списка литературы
- `DocumentService.cs` - Импорт/экспорт документов
- `DataService.cs` - CRUD операции с базой данных
- `DatabaseInitializer.cs` - Инициализация и настройка БД

### **📊 МОДЕЛИ ДАННЫХ (Models)**
- `Document.cs` - Документ с контентом и метаданными
- `Project.cs` - Проект, содержащий документы
- `PlagiarismResult.cs` - Результат проверки плагиата
- `Citation.cs` - Цитата с источником
- `Source.cs` - Источник литературы

### **🗄️ ДАННЫЕ (Data)**
- `PlagiatContext.cs` - Entity Framework контекст для работы с БД

### **🔧 ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ**
- `PlagiarismLevelToColorConverter.cs` - Конвертер цветов для уровней плагиата

## 📦 **ЗАВИСИМОСТИ (NuGet пакеты)**

### **🎨 UI и Контролы**
- `HandyControls.3.6.0` - Современные UI компоненты
- `HandyControls.Lang.ru.3.6.0` - Русская локализация

### **🗄️ База данных**
- `EntityFramework.6.5.1` - ORM для работы с SQL Server

### **📄 Обработка документов**
- `DocumentFormat.OpenXml.2.20.0` - Работа с DOCX файлами
- `iTextSharp.5.5.13.3` - Работа с PDF файлами
- `HtmlAgilityPack.1.11.54` - Парсинг HTML

### **🌐 Сетевые запросы**
- `System.Net.Http.4.3.4` - HTTP клиент
- `Microsoft.Extensions.Http.6.0.0` - Расширения для HTTP
- `Newtonsoft.Json.13.0.3` - JSON сериализация

## 🎯 **ОСНОВНЫЕ ФУНКЦИИ**

1. **📝 Импорт документов** - DOCX, PDF, TXT, RTF, HTML
2. **🔍 Проверка плагиата** - Через Advego Plagiatus API
3. **🤖 AI перефразирование** - Через OpenRouter + DeepSeek
4. **📚 Управление цитатами** - Автоматическое обнаружение и форматирование
5. **📖 Генерация библиографии** - Различные стили цитирования
6. **💾 Сохранение проектов** - В базе данных SQL Server LocalDB

## 🚀 **ТЕХНОЛОГИИ**

- **Framework:** .NET Framework 4.8
- **UI:** WPF + HandyControls
- **База данных:** SQL Server LocalDB + Entity Framework 6
- **Язык:** C# 8.0
- **Архитектура:** MVVM (частично)
- **API:** Advego Plagiatus, OpenRouter
- **AI:** DeepSeek Chat v3.1

**Проект представляет собой полнофункциональное приложение для проверки плагиата с AI-перефразированием и управлением цитатами!** 🎉
