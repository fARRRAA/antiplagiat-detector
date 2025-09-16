# 🖥️ ИНСТРУКЦИИ ПО ОТКРЫТИЮ КОНСОЛИ В VISUAL STUDIO

## ✅ **АВТОМАТИЧЕСКОЕ ОТКРЫТИЕ КОНСОЛИ:**

Я добавил принудительное открытие консоли в приложение. Теперь при запуске автоматически откроется окно консоли.

### **Что добавлено в `App.xaml.cs`:**
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool AllocConsole();

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Принудительно открываем консоль для отладки
    AllocConsole();
    Console.WriteLine("=== АНТИПЛАГИАТ-ПОМОЩНИК ЗАПУЩЕН ===");
    Console.WriteLine("Консоль отладки активна");
    
    // ... остальной код
}
```

## 🚀 **КАК ЗАПУСТИТЬ С КОНСОЛЬЮ:**

### **1. Запуск через Visual Studio:**
- **Нажмите F5** или **Ctrl+F5**
- **Консоль откроется автоматически** в отдельном окне
- Вы увидите сообщение: `"=== АНТИПЛАГИАТ-ПОМОЩНИК ЗАПУЩЕН ==="`

### **2. Запуск через командную строку:**
```cmd
cd "C:\Users\Ильдар\source\repos\antiplagiat-detector\Plagiat\bin\Debug"
Plagiat.exe
```

## 📋 **ЧТО ВЫ УВИДИТЕ В КОНСОЛИ:**

### **При запуске приложения:**
```
=== АНТИПЛАГИАТ-ПОМОЩНИК ЗАПУЩЕН ===
Консоль отладки активна
```

### **При загрузке документа:**
```
Выбран документ: 02_high_plagiarism.txt
ID документа: 1
Контент документа (до загрузки): 2228 символов
Перед LoadDocument: document.Content.Length = 2228
LoadDocument вызван с документом: 02_high_plagiarism.txt
LoadDocument - контент документа: 'Искусственный интеллект...' (длина: 2228)
LoadDocument - контент пустой: False
LoadDocument - _currentDocument установлен: 02_high_plagiarism.txt
LoadDocument - _currentDocument.Content: 'Искусственный интеллект...' (длина: 2228)
Загружаем в RichTextBox: 2228 символов
Текст успешно загружен в RichTextBox: 2228 символов
После установки контента: _currentDocument.Content.Length = 2228
Перед вызовом UpdateWordCount: _currentDocument.Content.Length = 2228
UpdateWordCount вызван. _currentDocument: True
UpdateWordCount - _currentDocument.Title: 02_high_plagiarism.txt
UpdateWordCount - _currentDocument.Id: 1
UpdateWordCount - контент документа: 'Искусственный интеллект...' (длина: 2228)
UpdateWordCount - контент пустой: False
UpdateWordCount - подсчитано: 350 слов, 2228 символов
После вызова UpdateWordCount: _currentDocument.Content.Length = 2228
После LoadDocument: _currentDocument.Content.Length = 2228
```

## 🔍 **АЛЬТЕРНАТИВНЫЕ СПОСОБЫ (если консоль не открывается):**

### **1. Через меню Visual Studio:**
- **View** → **Output** (Вид → Вывод)
- В окне Output выберите **"Debug"** в выпадающем списке

### **2. Через окно Debug Output:**
- **Debug** → **Windows** → **Output**
- Выберите **"Debug"** в выпадающем списке

### **3. Через окно Immediate:**
- **Debug** → **Windows** → **Immediate**
- Здесь можно выполнять команды во время отладки

## 🎯 **ЧТО ИСКАТЬ В КОНСОЛИ:**

### **Если контент исчезает, вы увидите:**
```
LoadDocument - контент документа: 'Искусственный интеллект...' (длина: 2228)
UpdateWordCount - контент документа: '' (длина: 0)  ← ВОТ ЗДЕСЬ ПРОБЛЕМА!
```

### **Если всё работает правильно:**
```
LoadDocument - контент документа: 'Искусственный интеллект...' (длина: 2228)
UpdateWordCount - контент документа: 'Искусственный интеллект...' (длина: 2228)
UpdateWordCount - подсчитано: 350 слов, 2228 символов
```

## 🚀 **ГОТОВО К ТЕСТИРОВАНИЮ!**

### **Теперь:**
1. **Запустите приложение** (F5)
2. **Консоль откроется автоматически**
3. **Загрузите документ** "02_high_plagiarism.txt"
4. **Следите за сообщениями** в консоли
5. **Найдите место**, где контент исчезает

**Консоль теперь будет открываться автоматически при каждом запуске!** 🎉
