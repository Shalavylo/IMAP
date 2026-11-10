using MailKit;
using MailKit.Net.Imap;
using Microsoft.Win32;
using MimeKit;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SmtpClient = MailKit.Net.Smtp.SmtpClient;
namespace _07_homework
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string attachmentPath = null;
        ImapClient imapClient = new ImapClient();
        public MainWindow()
        {
            InitializeComponent();

            txtUsername.Text = "Vshalavylo@gmail.com";
            txtPassword.Password = "hrrb zwlk enyo fiau";
        }

        private void Click_ConnectImap(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text;
                string password = txtPassword.Password;

                if(imapClient.IsConnected)
                {
                    imapClient.Disconnect(true);
                }

                imapClient.Connect("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                imapClient.Authenticate(username, password);

                lstFolders.Items.Clear();
                foreach (var folder in imapClient.GetFolder(imapClient.PersonalNamespaces[0]))
                {
                    lstFolders.Items.Add(folder);
                }
                MessageBox.Show("Успішно підключено Imap!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка підключення IMAP: {ex.Message}");
            }
        }

        private void LstFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lstFolders.SelectedItems is IMailFolder folder)
            {
                try
                {
                    folder.Open(FolderAccess.ReadOnly);
                    var item = folder.Fetch(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate);
                    lstMessages.ItemsSource = item;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка читання папки: {ex.Message}");
                }
            }
        }

        private void LstMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lstMessages.SelectedItem is IMessageSummary summary && lstFolders.SelectedItem is IMailFolder folder)
            {
                try
                {
                    var message = folder.GetMessage(summary.UniqueId);

                    lblMailFrom.Text = $"Від: {message.From}";
                    lblMailDate.Text = $"Дата: {message.Date}";
                    lblMailSubject.Text = $"Тема: {message.Subject}";
                    txtMailBody.Text = message.TextBody ?? message.HtmlBody;

                    lstAttachments.Items.Clear();
                    foreach (var attachment in message.Attachments)
                    {
                        lstAttachments.Items.Add(attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завантаження листа: {ex.Message}");
                }
            }
        }

        private void Click_AttachFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                attachmentPath = openFileDialog.FileName;
                lblAttachedFileName.Text = System.IO.Path.GetFileName(attachmentPath);
            }
        }

        private void Click_MessageSend(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text;
                string password = txtPassword.Password;
                string recipient = txtRecipient.Text;
                string subject = txtSubject.Text;
                string bodyText = txtBody.Text;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("", username));
                message.To.Add(new MailboxAddress("", recipient));
                message.Subject = subject;

                if(chkHighPriority.IsChecked == true)
                {
                    message.Importance = MessageImportance.High;
                }

                var builder = new BodyBuilder
                {
                    TextBody = bodyText
                };

                if(!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    builder.Attachments.Add(attachmentPath);
                }

                message.Body = builder.ToMessageBody();
                using(var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                    client.Authenticate(username, password);
                    client.Send(message);
                    client.Disconnect(true);
                }
                MessageBox.Show("Лист успішно надіслано!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка відправки: {ex.Message}");
            }
        }
    }
}