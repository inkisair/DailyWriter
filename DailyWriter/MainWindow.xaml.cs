using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Threading;
using System.Windows.Forms;

namespace DailyWriter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DailyWriter");

        string saveLocation = "";
        int wordTarget = 750;
        string currentDateTime = null;

        public string SaveLocation {
            get { if (saveLocation == "") return appDataPath; return saveLocation;}
            set { saveLocation = value; }
        }

        public string SettingsPath
        {
            get { return Path.Combine(appDataPath, "settings.txt"); }
        }

        public string AutosavePath
        {
            get { return Path.Combine(appDataPath, "autosave.rtf"); }
        }

        public string AutodatePath
        {
            get { return Path.Combine(appDataPath, "savedate.txt"); }
        }



        public MainWindow()
        {
            InitializeComponent();
            System.IO.Directory.CreateDirectory(appDataPath);
            if (File.Exists(SettingsPath))
                ReadPreferences();
            
            if (saveLocation == appDataPath)
                chooseLocation(true);

            if (SaveLocation == appDataPath)
               System.Windows.MessageBox.Show("You haven't chosen a save location. You can change the location from the menu, until then files will save in DailyWriter's appdata folder");
                

            cmbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            cmbFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            dtp.SelectedDate = DateTime.Today; //check for clash with save file //this automatically calls loadsavedfileifexists
            dtp.DisplayDateEnd = DateTime.Today;

            //load settings

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
           SaveFile(true);
            int wordcount = CountWords(StringFromRichTextBox());
            float value = (float)wordcount/(float)wordTarget;
            progressbar.Value = value;
            progresslabel.Content = wordcount.ToString() + "/";
        }

        private void rtbEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                //all code for the rich text editor toolbar is from https://www.wpf-tutorial.com/rich-text-controls/how-to-creating-a-rich-text-editor/
                object temp = rtbEditor.Selection.GetPropertyValue(Inline.FontWeightProperty);
                btnBold.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontWeights.Bold));
                temp = rtbEditor.Selection.GetPropertyValue(Inline.FontStyleProperty);
                btnItalic.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontStyles.Italic));
                temp = rtbEditor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                btnUnderline.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextDecorations.Underline));

                temp = rtbEditor.Selection.GetPropertyValue(Inline.FontFamilyProperty);
                cmbFontFamily.SelectedItem = temp;
                temp = rtbEditor.Selection.GetPropertyValue(Inline.FontSizeProperty);
                cmbFontSize.Text = temp.ToString();
            }

            catch (Exception ex)
            {
                errorlabel.Content = "Failed to change text properties";
            }
        }

        string StringFromRichTextBox()
        {
            TextRange textRange = new TextRange(
                // TextPointer to the start of content in the RichTextBox.
                rtbEditor.Document.ContentStart,
                // TextPointer to the end of content in the RichTextBox.
                rtbEditor.Document.ContentEnd
            );

            // The Text property on a TextRange object returns a string
            // representing the plain text content of the TextRange.
            return textRange.Text;
        }

        private void cmbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbFontFamily.SelectedItem != null)
                    rtbEditor.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, cmbFontFamily.SelectedItem);
            }
            catch (Exception ex)
            {
                errorlabel.Content = "Failed to change font";
            }
        }

        private void cmbFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                rtbEditor.Selection.ApplyPropertyValue(Inline.FontSizeProperty, cmbFontSize.Text);
            }
            catch (Exception ex)
            {
                errorlabel.Content = "Failed to change size";
            }
        }

        public static int CountWords(string s)
        {
            //source: https://www.dotnetperls.com/word-count
            MatchCollection collection = Regex.Matches(s, @"[\S]+");
            return collection.Count;
        }

        private void Dtp_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            string datestring = GetSelectedDate();
            //save existing file and delete temp
            if (!string.Equals(currentDateTime, datestring))
            {
                SaveFile();
                ClearTextbox();
                LoadSavedFileIfExists();
            }
            currentDateTime = datestring;
        }

        private string GetSelectedDate()
        {
            return dtp.SelectedDate.Value.ToString("yyyy-MM-dd");
        }

        private void ClearTextbox()
        {
            TextRange range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
            range.Text = " ";
        }

        private void LoadSavedFileIfExists()
        {
            string datestring = GetSelectedDate();
            try
            {
                string filename = System.IO.Path.Combine(SaveLocation, datestring + ".rtf");
                string testdate = "";
                if (File.Exists(AutosavePath))
                {
                    testdate = System.IO.File.ReadAllText(AutodatePath);
                }

                if (string.Equals(testdate, datestring))
                {
                    FileStream fileStream = new FileStream(AutosavePath, FileMode.Open, FileAccess.Read);
                    TextRange range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Load(fileStream, System.Windows.DataFormats.Rtf);
                    fileStream.Close();
                }
                else if (File.Exists(filename))
                {

                    FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                    TextRange range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Load(fileStream, System.Windows.DataFormats.Rtf);
                    fileStream.Close();
                }
            }
            catch(Exception ex)
            {
                errorlabel.Content = "Failed to load saved file, probably because it's open in a different program";
            }
            currentDateTime = datestring;
        }

        private void SaveFile(bool autosave=false)
        {
            try
            {
                string filename;
                string datestring = currentDateTime;
                if (autosave)
                    filename = AutosavePath;
                else
                    filename = System.IO.Path.Combine(SaveLocation, datestring + ".rtf");
                FileStream fileStream = new FileStream(filename, FileMode.OpenOrCreate);
                TextRange range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                range.Save(fileStream, System.Windows.DataFormats.Rtf);
                fileStream.Close();
                if (autosave)
                {
                    // WriteAllText creates a file, writes the specified string to the file,
                    // and then closes the file.    You do NOT need to call Flush() or Close().
                    System.IO.File.WriteAllText(AutodatePath, datestring);

                }
            }
            catch (Exception e)
            {
                if (autosave)
                    errorlabel.Content = "Failed to save file, probably because it's open in a different program";
                else
                    throw e;
            }
        }

        private void RtbEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
        //1. save file:

        //2. update wordcount:
        }

        private void SaveLocation_Click(object sender, RoutedEventArgs e)
        {
            chooseLocation(false);
        }

        private string chooseLocation(bool startup)
        {
            string desc = "Select the directory where you would like your writing files to be saved";
            if (startup)
                desc = "Welcome to the DailyWriter app! Please select the directory where you would like your writing files to be saved";
            

            FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = saveLocation;
            dialog.Description = desc;
            
            dialog.ShowDialog();
            string loc = dialog.SelectedPath;
            if (loc!="")
                SaveLocation = loc;
            SavePreferences();
            return loc;
        }


        private void SavePreferences()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(SettingsPath))
            {
                file.WriteLine(wordTarget.ToString());
                file.WriteLine(SaveLocation);
            }

        }

        private string ReadPreferences()
        {
            string[] lines = System.IO.File.ReadAllLines(SettingsPath);
            wordTarget = Int32.Parse(lines[0]);
            wordcountGoalTextBox.Text = lines[0];
            string loc= lines[1];
            SaveLocation=loc;
            return loc;

        }


        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("DailyWriter was created by reddit user inkisair for the r/writingdaily subreddit\n" +
                "You can PM bug reports etc on reddit");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save, delete backup file
            try
            {
                SaveFile();
                if (File.Exists(AutosavePath))
                    System.IO.File.Delete(AutosavePath);
            }
            catch (Exception ex)
            {
                string msg = "Failed to automatically save text (possibly the save file is open in another program). Close without saving?";
                MessageBoxResult result =
                  System.Windows.MessageBox.Show(
                    msg,
                    "DailyWriter",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    // If user doesn't want to close, cancel closure
                    e.Cancel = true;
                }
            }
        }

        private void WordcountGoalTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            wordTarget = Convert.ToInt32(wordcountGoalTextBox.Text);
            SavePreferences();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Input.Keyboard.ClearFocus();
        }
    }
}
