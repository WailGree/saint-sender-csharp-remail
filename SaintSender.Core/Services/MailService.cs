﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Xml.Serialization;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MimeKit;
using SaintSender.Core.Interfaces;
using SaintSender.Core.Models;

namespace SaintSender.Core.Services
{
    public class MailService : IBackup
    {
        public static List<MimeMessage> GetMails(string username, string password)
        {
            List<MimeMessage> emails = new List<MimeMessage>();
            using (var client = new ImapClient())
            {
                client.Connect("imap.gmail.com", 993, true);
                client.Authenticate(username, password);
                //The Inbox folder is always available on all IMAP servers...
                IMailFolder inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadOnly);
                foreach (MimeMessage mail in inbox)
                {
                    emails.Add(mail);
                }

                client.Disconnect(true);
            }

            return emails;
        }

        public static void SendNewEmail(string username, string password, string text, string subject, string toMail)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(username));
            message.To.Add(new MailboxAddress(toMail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = text
            };

            using (var client = new SmtpClient())
            {
                client.Connect("imap.gmail.com", 465, true);

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(username, password);

                client.Send(message);
                client.Disconnect(true);
            }
        }

        public static bool IsCorrectLoginCredentials(string username, string password)
        {
            try
            {
                using (var client = new ImapClient())
                {
                    client.Connect("imap.gmail.com", 993, true);
                    client.Authenticate(username, password);
                    client.Disconnect(true);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void NewBackup(List<MimeMessage> emails, string path = "EmailBackup.xml")
        {
            IsolatedStorageFile isoStore =
                IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if (isoStore.FileExists(path))
            {
                isoStore.DeleteFile(path);
            }

            using (IsolatedStorageFileStream isoStream =
                new IsolatedStorageFileStream(path, FileMode.CreateNew, isoStore))
            {
                using (StreamWriter sw = new StreamWriter(isoStream))
                {
                    XmlSerializer xs = new XmlSerializer(typeof(List<MimeMessage>));
                    xs.Serialize(sw, emails);
                }

                string filePath = isoStream.GetType()
                    .GetField("m_FullPath", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(isoStream)
                    .ToString();
                Console.WriteLine(filePath);
            }
        }

        public List<MimeMessage> LoadBackup(string path = "EmailBackup.xml")
        {
            IsolatedStorageFile isoStore =
                IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if (isoStore.FileExists(path))
            {
                using (IsolatedStorageFileStream isoStream =
                    new IsolatedStorageFileStream(path, FileMode.Open, isoStore))
                {
                    using (StreamReader sw = new StreamReader(isoStream))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(List<MimeMessage>));
                        return (List<MimeMessage>)xs.Deserialize(sw);
                    }
                }
            }

            return null;
        }
    }
}