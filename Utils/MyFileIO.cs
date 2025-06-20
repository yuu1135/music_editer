﻿using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;

using music_editer.Models;

namespace music_editer.Utils {
    internal class MyFileIO {
        public static void SaveToMyFile(List<Note> notes, double bpm, string audioPath, string myfilePath) {
            using (FileStream zipToOpen = new FileStream(myfilePath, FileMode.Create)) {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create)) {
                    // JSONをZIP内にchart.jsonという名前で追加
                    var jsonEntry = archive.CreateEntry("chart.json");
                    var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
                    using (var writer = new StreamWriter(jsonEntry.Open())) {
                        writer.Write(json);
                    }

                    // JSONをZIP内にchart.jsonという名前で追加
                    var jsonEntry2 = archive.CreateEntry("chartstatus.json");
                    var json2 = JsonSerializer.Serialize(bpm);
                    using (var writer = new StreamWriter(jsonEntry2.Open())) {
                        writer.Write(json2);
                    }


                    // 音楽ファイルを追加（拡張子そのまま）
                    string audioFileName = Path.GetFileName(audioPath);
                    archive.CreateEntryFromFile(audioPath, audioFileName);
                }
            }
        }

        public static void LoadFromZipToString(string zipPath, out List<Note> jsonContent, out double bpm, out string audioTempPath) {
            string tempFolder = Path.Combine(Path.GetTempPath(), "ChartEditorTemp");
            Directory.CreateDirectory(tempFolder);

            jsonContent = null;
            bpm = 120;
            audioTempPath = null;
            

            using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
                foreach (var entry in archive.Entries) {
                    string destinationPath = Path.Combine(tempFolder, entry.Name);

                    if (entry.Name == "chart.json") {
                        using (var reader = new StreamReader(entry.Open())) {
                            var json = reader.ReadToEnd();
                            jsonContent = JsonSerializer.Deserialize<List<Note>>(json) ?? new List<Note>();
                        }

                    } else if (entry.Name == "chartstatus.json") {
                        using (var reader = new StreamReader(entry.Open())) {
                            var json = reader.ReadToEnd();
                            bpm = JsonSerializer.Deserialize<double>(json);
                        }

                    } else if (entry.Name.ToLower().EndsWith(".mp3") || entry.Name.ToLower().EndsWith(".wav")) {
                        entry.ExtractToFile(destinationPath, true);
                        audioTempPath = destinationPath;
                    }
                }
            }

            if (jsonContent == null || audioTempPath == null)
                throw new Exception("ZIPファイルに必要なファイル（JSONまたは音声）が見つかりません。");
        }
    }
}
