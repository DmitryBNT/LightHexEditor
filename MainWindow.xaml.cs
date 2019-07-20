using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Начало блока пользовательских функций
using System.Runtime.InteropServices;   // DllImport
using Microsoft.Win32;                  // Dialog
using System.Security.Cryptography;     // MD5
using System.IO;                        // File
using System.ComponentModel;            // BackgroundWorker
using System.Threading;                 //Thread
// Конец блока пользовательских функций

namespace LightHexEditor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        // Начальная инициализация пути до входного файла
        string FilePathIn = "";
        // Объект backgroundWorker
        private BackgroundWorker backgroundWorker = null;
        // Объявление переменной для расчёта времени получения кода
        DateTime DateOfStartGetHex;

        public MainWindow()
        {
            InitializeComponent();

            // Настройка элементов графического интерфейса //
            
            // Скрытие подписи и самого пути до файла
            LabelPath.Visibility = Visibility.Hidden;
            TextBlockPathToFile.Visibility = Visibility.Hidden;
            // Отклюбчение кнопки остановки обработки файла
            StopOperationButton.IsEnabled = false;
            
            // Конец настройки элементов //

            // Настройка backgroundWorker //

            // Объявление переменной
            backgroundWorker = new BackgroundWorker();

            // РАзрешение методов
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;

            // Задание обработчиков
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            
            // Конец настройки //
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int CheckEnd = 0;
            int StartPositionOfReadFile = 0;
            int FirstReading = 1;
            int NumberOfCycle = 0;
            int CleaningFile = 1;

            while (CheckEnd != 1)
            {
                if (backgroundWorker.CancellationPending == true)
                {
                    break;
                }

                CheckEnd = ImportantBase(StartPositionOfReadFile, FirstReading, CleaningFile);
                StartPositionOfReadFile += 3200;

                FirstReading = 0;
                CleaningFile = 0;

                NumberOfCycle++;
                backgroundWorker.ReportProgress(NumberOfCycle);

                Thread.Sleep(1);
            }

        }
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LHEProgressBar.Value = LHEProgressBar.Maximum;
            StopOperationButton.IsEnabled = false;

            TextBoxConsole.AppendText("Извлечение hex кода ЗАВЕРШЕНО. ");

            DateTime DateOfEndGetHex = DateTime.Now;
            TimeSpan TimeGetHexCode =  DateOfEndGetHex - DateOfStartGetHex;

            TextBoxConsole.AppendText("На извлечение кода потребовалось: " + TimeGetHexCode.TotalSeconds + " секунд.\n");
        }
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LHEProgressBar.Value = e.ProgressPercentage;
        }

        // Начало секции импорта
        [DllImport("lhe_32.dll", CallingConvention = CallingConvention.Cdecl)]

        public static extern int ImportantBase(int StartPosition, int FirstStart, int CleanFile);
        [DllImport("lhe_32.dll", CallingConvention = CallingConvention.Cdecl)]

        public static extern void PathToFile(string FullPathFileIn, string FullPathFileOut);
        [DllImport("lhe_32.dll", CallingConvention = CallingConvention.Cdecl)]

        public static extern int SizeOfFile();
        // Конец секции импорта

        private void Button_Click_GetHex(object sender, RoutedEventArgs e)
        {
            // Переменная для проверки целостности
            int result_md5 = -1;

            // Начало проверки существования .dll файла
            if (File.Exists("lhe_32.dll") == true)
            {
                TextBoxConsole.AppendText("Файл lhe_32.dll существует. ");
                TextBoxConsole.AppendText("Начата проверка...\n");
                result_md5 = CheckMD5();
            }
            else
            {
                TextBoxConsole.AppendText("Файла lhe_32.dll не существует.\n");
            }
            // Конец проверки


            // Вызов функции из .dll (если .dll нет, то будет вызван обработчик исключения

            try
            {
                if (FilePathIn == "")
                {
                    TextBoxConsole.AppendText("Путь до файла ПУСТ - проверьте путь.\n");
                }
                else
                {
                    if (result_md5 == 0)
                    {
                        PathToFile(FilePathIn, Environment.CurrentDirectory + "//TempHexFile.txt");

                        DateTime DateOfStartStat, DateOfEndStat;
                        DateOfStartStat = DateTime.Now;

                        int SizeFile = SizeOfFile();
                        TextBoxStatistics.AppendText("Размер файла: " + SizeFile +" байт.\n");

                        double NumOfAllLine = (SizeFile / 16.0);
                        TextBoxStatistics.AppendText("Всего строк: " + Math.Ceiling(NumOfAllLine) + ".\n");
                        double NumOfFullLine = (SizeFile / 16.0);
                        TextBoxStatistics.AppendText("Количество полных строк: " + Math.Floor(NumOfFullLine) + ".\n");

                        // Если байт 1700, а чтение 1600, то будет 1,0625: Ceiling = 2, Floor = 1
                        double StageForProgressBar = (SizeFile / 3200.0);
                        TextBoxStatistics.AppendText("Делений у прогресс бара: " + Math.Ceiling(StageForProgressBar) + ".\n");
                        LHEProgressBar.Minimum = 0;
                        LHEProgressBar.Maximum = Math.Ceiling(StageForProgressBar);

                        DateOfEndStat = DateTime.Now;
                        TimeSpan TimeInWork = DateOfEndStat - DateOfStartStat;

                        TextBoxConsole.AppendText("На анализ потрачено: " + (TimeInWork.TotalSeconds) + " секунд.\n");

                        MessageBoxResult result = MessageBox.Show(
                            this, 
                            "Начать операцию извлечения кода?", 
                            "Получение кода", MessageBoxButton.YesNo, 
                            MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            TextBoxConsole.AppendText("Разрешение на операцию извлечения - дано.\n");

                            //******************************* ДОЛГАЯ ОПЕРАЦИЯ *******************************\\

                            StopOperationButton.IsEnabled = true;

                            DateOfStartGetHex = DateTime.Now;

                            backgroundWorker.RunWorkerAsync();

                            //******************************* ДОЛГАЯ ОПЕРАЦИЯ *******************************\\
                        }
                        else
                        {
                            TextBoxConsole.AppendText("Разрешение на операцию извлечения НЕ дано.\n");
                        }
                    }
                    else
                    {
                        TextBoxConsole.AppendText("Необходимо вновь получить lhe_32.dll - нарушена его целостность.");
                        MessageBox.Show(this, "Файл .dll повреждён", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }
                
            }
            catch (DllNotFoundException text)
            {
                MessageBox.Show(text.Message);
            }
            
            // Конец секции вызова и проверки
        }

        private void Button_Click_PathOpen(object sender, RoutedEventArgs e)
        {
            // Начало блока диалогового окна для получения полного пути до файла //

            OpenFileDialog FPDialog = new OpenFileDialog();
            if (FPDialog.ShowDialog() == true)
            {
                FilePathIn = FPDialog.FileName;
            }
            if (FilePathIn != "")
            {
                TextBlockPathToFile.Text = FilePathIn;
                LabelPath.Visibility = Visibility.Visible;
                TextBlockPathToFile.Visibility = Visibility.Visible;
            }
            
            // Конец блока //
        }

        private int CheckMD5()
        {
            // Проверка целостности .dll файла

            string MD5HashCode;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead("lhe_32.dll"))
                {
                    byte[] Ex = md5.ComputeHash(stream);

                    string Tmp;
                    MD5HashCode = "";

                    for (int i = 0; i < Ex.Length; i++)
                    {
                        Tmp = Ex[i].ToString("X2");
                        MD5HashCode = string.Concat(MD5HashCode, Tmp);
                    }
                }
            }
            string ReallyMD5HashCode = "29CC1A1CA12FAE6B1F72BA7B01F19775";

            if (string.Compare(ReallyMD5HashCode, MD5HashCode) == 0)
            {
                TextBoxConsole.AppendText("Файл lhe_32.dll успешно прошёл проверку целостности.\n");
                return 0;
            }
            else
            {
                TextBoxConsole.AppendText("Файл lhe_32.dll повреждён.\n");
                return 1;
            }
            // Конец проверки
        }

        private void Button_Click_CleanConsole(object sender, RoutedEventArgs e)
        {
            TextBoxConsole.Clear();
        }

        private void Button_Click_SaveFile(object sender, RoutedEventArgs e)
        {
            // Объявление переменной потока
            Stream myStream;
            // Объявление переменной окна
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            // Начало блока установки параметров диалогового окна //
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.Title = "Сохранить Hex код в файл";
            // Конец блока //

            if (saveFileDialog1.ShowDialog() == true)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    myStream.Write(System.Text.Encoding.Default.GetBytes(HexCodeViewer.Text), 0, (HexCodeViewer.Text).Length);
                    myStream.Close();
                }
            }
        }

        private void Button_Click_LoadHex(object sender, RoutedEventArgs e)
        {
            // Полный путь откуда идет запуск .exe - Environment.CurrentDirectory
            string FullPathOfTempFile = Environment.CurrentDirectory + "//TempHexFile.txt";
            if (File.Exists(FullPathOfTempFile) == true)
            {
                FileInfo Test = new FileInfo(FullPathOfTempFile);
                TextBoxConsole.AppendText("Размер файла: " + Test.Length + " байт.\n");
                if (Test.Length > 3000000)
                {
                    string FullMesg = string.Concat("Обратите внимание: размер файла очень велик и равен" + 
                        Test.Length + 
                        " байт. Его закгрузка будет длиться приблизительно: " +
                        (Math.Round(Test.Length / 1000000.0) * 3.33) +
                        " секунд(-ы). Вы уверены, что хотите продолжить?");

                    MessageBoxResult result = MessageBox.Show(
                            this,
                            FullMesg,
                            "Операция загрузки кода в окно программы", MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        HexCodeViewer.Text = File.ReadAllText(FullPathOfTempFile);
                    }
                }
                else
                {
                    HexCodeViewer.Text = File.ReadAllText(FullPathOfTempFile);
                }
            }
            else
            {
                MessageBox.Show(
                            this,
                            "Извлечение не проводилось или файл с извлечённым кодом повреждён",
                            "Ошибка загрузки кода в окно", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            }
        }

        private void Button_Click_DeleteTempData(object sender, RoutedEventArgs e)
        {
            // Полный путь откуда идет запуск .exe - Environment.CurrentDirectory
            string FullPathOfTempFile = Environment.CurrentDirectory + "//TempHexFile.txt";
            if (File.Exists(FullPathOfTempFile) == true)
            {
                File.Delete(FullPathOfTempFile);
                TextBoxConsole.AppendText("Был удалён 1 временный файл.\n");
            }
            else
            {
                TextBoxConsole.AppendText("Временных файлов не обнаружено.\n");
            }
        }

        private void Button_Click_StopProcess(object sender, RoutedEventArgs e)
        {
            // Остановка обработки
            backgroundWorker.CancelAsync();
        }

        private void Button_Click_SaveTempFile(object sender, RoutedEventArgs e)
        {
            // Временный файл перемещается в директорию сохранения со штампом времени
            string SavePath = Environment.CurrentDirectory + "\\Saved";

            DateTime DateNow = DateTime.Now;
            string FullName = string.Concat("\\HexCode_" + DateNow.ToString("MM.dd.yyyy_HH.mm.ss") + ".txt");

            // Полный путь откуда идет запуск .exe - Environment.CurrentDirectory
            string FullPathOfTempFile = Environment.CurrentDirectory + "//TempHexFile.txt";
            if (File.Exists(FullPathOfTempFile) == true)
            {
                if (Directory.Exists(SavePath) != true)
                {
                    Directory.CreateDirectory(SavePath);
                    TextBoxConsole.AppendText("Папки «Saved» не существовало - она была создана.\n");
                }
                File.Move(Environment.CurrentDirectory + "\\TempHexFile.txt", SavePath + FullName);
                TextBoxConsole.AppendText("Файл с hex кодом сохранён по пути:\n");
                TextBoxConsole.AppendText(string.Concat(SavePath + FullName + "\n"));
            }
            else
            {
               MessageBox.Show(
                            this,
                            "Извлечение не проводилось или файл с извлечённым кодом повреждён",
                            "Ошибка сохранения кода", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            }
        }

        private void Button_Click_CleanCodeWindow(object sender, RoutedEventArgs e)
        {
            HexCodeViewer.Clear();
        }

        private void Button_Click_CleanStatWindow(object sender, RoutedEventArgs e)
        {
            TextBoxStatistics.Clear();
        }
    }
}
