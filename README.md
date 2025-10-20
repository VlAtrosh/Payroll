# 💼 Payroll Management System

<div align="center">

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/Microsoft_SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Windows Forms](https://img.shields.io/badge/Windows_Forms-5C2D91?style=for-the-badge&logo=.net&logoColor=white)

**Система автоматизации расчета заработной платы с полным циклом управления сотрудниками и отделами**

[Особенности](#-особенности) • [Разработчики](#-разработчики)

</div>

## 🚀 О проекте

Учебный проект, разработанный в рамках курса **"Технология доступа к базам данных ADO.NET"**. Система автоматизирует процессы расчета заработной платы, минимизируя ошибки ручного расчета и повышая эффективность работы предприятия.

### 🎯 Ключевые цели
- ✅ **Снижение временных затрат** на расчет ЗП на 70%
- ✅ **Минимизация ошибок** расчета до 0.1%
- ✅ **Повышение прозрачности** расчетов
- ✅ **Ускорение формирования** отчетности

## ✨ Особенности

### 🛠️ Функциональность
- **Полный цикл CRUD-операций** - управление сотрудниками, отделами, должностями
- **Расчет заработной платы** - автоматическое начисление и удержание
- **Управление периодами** - гибкая система расчетных периодов
- **Валидация данных** - проверка корректности вводимой информации

### 🎨 Интерфейс
- **Интуитивный дизайн** - Windows Forms с тематическим оформлением
- **Табличное представление** - удобный просмотр и сортировка данных
- **Динамические формы** - умная валидация и подсказки

### 🗃️ База данных
- **Нормализованная структура** - соответствие 3NF
- **SQL Server Integration** - стабильное подключение
- **Оптимизированные запросы** - быстрое выполнение операций

## 🗃️ Структура базы данных

![у22у](https://github.com/user-attachments/assets/3a4bddc1-fdc4-452c-a6eb-2e3a52e5d044)


### Основные таблицы
| Таблица | Описание |
|---------|-----------|
| **Department** | Структурные подразделения компании |
| **Position** | Штатные должности и базовые оклады |
| **Employee** | Основная информация о сотрудниках |
| **PayPeriod** | Периоды расчета зарплаты |
| **Payroll** | Основные расчеты зарплаты |
| **Earning** | Типы начислений (премии, надбавки) |
| **Deduction** | Типы удержаний (налоги, штрафы) |

## 🖥️ Демо интерфейса

### Форма авторизации
<img width="537" height="355" alt="Снимок экрана (141)" src="https://github.com/user-attachments/assets/e747b6d0-03cf-4d15-be68-7b9346444397" />

*Подключение к SQL Server с проверкой учетных данных*

### Главное окно
<img width="1920" height="415" alt="Снимок экрана (142)" src="https://github.com/user-attachments/assets/2473ac17-7298-49c4-aac0-d324228542d3" />

*Многофункциональный интерфейс с вкладками для управления всеми сущностями системы*

## 🛠️ Технологический стек

**Backend:** 
![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ADO.NET](https://img.shields.io/badge/ADO.NET-5C2D91?style=flat-square&logo=.net&logoColor=white)

**Database:** 
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat-square&logo=microsoft-sql-server&logoColor=white)

**UI:** 
![Windows Forms](https://img.shields.io/badge/Windows_Forms-5C2D91?style=flat-square&logo=.net&logoColor=white)

## 📦 Установка и запуск

### Предварительные требования
- Windows 10/11
- .NET Framework 4.7.2 или выше
- Microsoft SQL Server (Express или выше)
- Visual Studio 2019+

## 👥 Разработчики
Атрошенко Владислав

Копычев Матвей

Зеленов Данил

Учебное заведение: 🎓 Компьютерная академия Top, Санкт-Петербург
Год разработки: 2025
