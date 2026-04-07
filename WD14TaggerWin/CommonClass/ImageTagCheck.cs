using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Png.Chunks;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Diagnostics;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using UtfUnknown;

namespace CommonClass
{
    internal class ImageTagCheck
    {
        /// <summary>
        /// 画像情報の取得
        /// </summary>
        /// <param name="imagePath">画像パス</param>
        /// <returns>(ポジティブタグ,ネガティブタグ,設定)</returns>
        public static (string, string, string, string) CheckImageInfo(Image<Rgba32> img, string imagePath)
        {
            string imageSize = " "+ img.Width.ToString() + "x" + img.Height.ToString();
            string title = string.Empty;
            string positivePrompt = string.Empty;
            string negativePrompt = string.Empty;
            string sourceString = string.Empty;

            // 対応画像フォーマットチェック
            if (img.Metadata.DecodedImageFormat == PngFormat.Instance)
            {
                // PNGフォーマットの場合
                title = " PNG";
                PngMetadata pngMeta = img.Metadata.GetPngMetadata();
                foreach (PngTextData txt in pngMeta.TextData)
                {
                    // 可能性がある文字列を内容チェック
                    if ((txt.Keyword.ToLower() == "parameters") || (txt.Keyword.ToLower() == "prompt") || (txt.Keyword.ToLower() == "workflow") || (txt.Keyword.ToLower() == "Comment"))
                    {
                        sourceString = txt.Value;
                        (positivePrompt, negativePrompt) = ParseMetadatas(sourceString, ref title);
                        if (positivePrompt != string.Empty) return (title + imageSize, positivePrompt, negativePrompt, sourceString);
                    }
                }

                // どれもタグとして無効の場合
                sourceString = string.Empty;
            }
            else if (img.Metadata.DecodedImageFormat == JpegFormat.Instance)
            {
                // Jpegフォーマットの場合
                title = " JPG";
                ExifProfile? exifProfile = img.Metadata.ExifProfile;
                if (exifProfile != null)
                {
                    // ImageSharpのEncodedString文字コードチェックが甘いので化ける
                    // 正直言ってEXIF解析はImageSharpを窓から投げ捨てるレベル
                    IExifValue<EncodedString>? UserComment;
                    exifProfile.TryGetValue(ExifTag.UserComment, out UserComment);
                    if (UserComment != null) sourceString = CheckString((string)UserComment.Value);
                }
            }
            else if (img.Metadata.DecodedImageFormat == WebpFormat.Instance)
            {
                // Webpフォーマットの場合
                title = " Webp";
                ExifProfile? exifProfile = img.Metadata.ExifProfile;
                if (exifProfile != null)
                {
                    // ImageSharpのEncodedString文字コードチェックが甘いので化ける
                    IExifValue<EncodedString>? UserComment;
                    exifProfile.TryGetValue(ExifTag.UserComment, out UserComment);
                    if (UserComment != null) sourceString = CheckString((string)UserComment.Value);
                }
            }
            else
            {
                title = " -";
            }

            // 取得したsourceStringの種別チェック
            if (sourceString != string.Empty)
            {
                (positivePrompt, negativePrompt) = ParseMetadatas(sourceString, ref title);
            }
            // メタデータに有意な文字列がない
            else
            {
                // ステルス情報のチェック
                sourceString = GetStealthInfo(img);
                if (sourceString != string.Empty)
                {
                    title = title + " Stealth";
                    (positivePrompt, negativePrompt) = ParseMetadatas(sourceString, ref title);
                }
                else title = "none";
            }

            return (title + imageSize, positivePrompt, negativePrompt, sourceString);
        }

        /// <summary>
        /// メタデータの解析
        /// </summary>
        /// <param name="sourceString">取得文字列</param>
        /// <param name="title">出力タイトル文字列</param>
        /// <returns>(positivePrompt,negativePrompt)</returns>
        private static (string, string) ParseMetadatas(string sourceString, ref string title)
        {
            string positivePrompt = string.Empty;
            string negativePrompt = string.Empty;

            // 取得したsourceStringの種別チェック
            if (sourceString != string.Empty)
            {
                // JSONかチェック
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(sourceString))
                    {
                        // ComfyUIかNvelAIかチェック
                        JsonElement root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            bool isNovelAI = false;

                            // ルート辞書に「Software」キーがある
                            JsonElement val;
                            if (root.TryGetProperty("Software", out val))
                            {
                                // NovelAIチェック
                                string? softwareStr = val.GetString();
                                if ((softwareStr != null) && (softwareStr == "NovelAI"))
                                {
                                    // コメントがある場合
                                    if (root.TryGetProperty("Comment", out val))
                                    {
                                        string? escapedStr = val.GetString();
                                        if (escapedStr != null)
                                        {
                                            // inner JSONの復元とチェック
                                            (positivePrompt, negativePrompt) = ParseNovelAI(escapedStr);
                                            if (positivePrompt != string.Empty) title = "NovelAI" + title;
                                            isNovelAI = true;
                                        }
                                    }
                                }
                            }

                            // NovelAIチェックが通らなかった
                            if (isNovelAI == false)
                            {
                                // Comfyとして処理を試みる
                                (positivePrompt, negativePrompt) = ParseComfyUIAI(sourceString);
                                if (positivePrompt != string.Empty) title = "ComfyUI" + title;
                            }
                        }
                    }
                }
                catch
                {
                    // 非JSON
                    (positivePrompt, negativePrompt) = ParseAutomatic1111(sourceString);
                    if (positivePrompt != string.Empty) title = "Automatic1111" + title;
                }
            }

            return (positivePrompt, negativePrompt);
        }



        /// <summary>
        /// Automatic1111 PNG Infoチェック
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        private static (string, string) ParseAutomatic1111(string sourceString)
        {
            string positiveTag = string.Empty;
            string negativeTag = string.Empty;

            // StableDiffusionのinfoか判定
            string[] stringParts = sourceString.Split("Negative prompt:");
            if (stringParts.Length > 1)
            {
                // Negative prompt:までがPositivePrompt
                positiveTag = stringParts[0];
                stringParts = stringParts[1].Split("Steps:");
                negativeTag = stringParts[0];

                return (positiveTag, negativeTag);
            }
            else if (stringParts.Length == 1)
            {
                // 行分割
                stringParts = stringParts[0].Split("Steps:");
                positiveTag = stringParts[0];
                return (positiveTag, negativeTag);
            }

            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// ComfyUIプロンプトチェック
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        private static (string, string) ParseComfyUIAI(string sourceString)
        {
            string positiveTag = string.Empty;
            string negativeTag = string.Empty;

            try
            {
                // Jsonパースを試みる
                using (JsonDocument doc = JsonDocument.Parse(sourceString))
                {
                    JsonElement root = doc.RootElement;

                    // ルートがオブジェクト
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        // 全ルートプロパティをチェック
                        foreach (JsonProperty property in root.EnumerateObject())
                        {
                            // property.Valueがオブジェクトの場合のみ処理
                            if (property.Value.ValueKind == JsonValueKind.Object)
                            {
                                // property.Valueのclass_typeエレメントを取得/エレメントが文字列かチェック
                                JsonElement classTypeEl;
                                if ((property.Value.TryGetProperty("class_type", out classTypeEl)) && (classTypeEl.ValueKind == JsonValueKind.String))
                                {
                                    // エレメント文字列がKSamplerの場合
                                    string? val = classTypeEl.GetString();
                                    if ((val != null) && (val == "KSampler"))
                                    {
                                        // property.Valueのinputsエレメントをを取得する
                                        JsonElement inputEl;
                                        if (property.Value.TryGetProperty("inputs", out inputEl))
                                        {
                                            if (inputEl.ValueKind == JsonValueKind.Object)
                                            {
                                                // positive promptのCLIPを取得
                                                positiveTag = GetClipText(root, inputEl, "positive");

                                                // negative promptのCLIPを取得
                                                negativeTag = GetClipText(root, inputEl, "negative");

                                                return (positiveTag, negativeTag);
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((property.Name.ToLower() == "prompt") && (property.Value.ValueKind == JsonValueKind.String))
                            {
                                string? res = property.Value.GetString();
                                positiveTag = res ?? string.Empty;
                            }
                            else if ((property.Name.ToLower() == "negative_prompt") && (property.Value.ValueKind == JsonValueKind.String))
                            {
                                string? res = property.Value.GetString();
                                negativeTag = res ?? string.Empty;
                            }
                        }
                    }
                }
            }
            catch { }

            return (positiveTag, negativeTag);
        }

        /// <summary>
        /// KサンプラーのinputエレメントからpromptKeyで指定するルートノードの配下のCLIPノードのinputs.textを取得
        /// </summary>
        /// <param name="root"></param>
        /// <param name="ksamplerInputEl"></param>
        /// <param name="promptKey"></param>
        /// <returns></returns>
        private static string GetClipText (JsonElement root, JsonElement ksamplerInputEl, string promptKey)
        {
            // promptのCLIPを取得
            JsonElement prompt;
            if (ksamplerInputEl.TryGetProperty(promptKey, out prompt))
            {
                if (prompt.ValueKind == JsonValueKind.Array)
                {
                    string? clipKey = prompt[0].ToString();
                    if (clipKey != null)
                    {
                        JsonElement clipEl;
                        if (root.TryGetProperty(clipKey, out clipEl))
                        {
                            if (clipEl.ValueKind == JsonValueKind.Object)
                            {
                                JsonElement clipInputEl;
                                if (clipEl.TryGetProperty("inputs", out clipInputEl))
                                {
                                    if (clipInputEl.ValueKind == JsonValueKind.Object)
                                    {
                                        JsonElement textEl;
                                        if (clipInputEl.TryGetProperty("text", out textEl))
                                        {
                                            if (textEl.ValueKind == JsonValueKind.String)
                                            {
                                                string? res = textEl.GetString();
                                                if (res != null) return res;
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// NovelAIコメントプロンプトチェック
        /// </summary>
        /// <param name="sourceString">Commentセクション</param>
        /// <returns></returns>
        private static (string, string) ParseNovelAI(string sourceString)
        {
            try
            {
                string positiveTag = string.Empty;
                string negativeTag = string.Empty;

                // Jsonパースを試みる
                using (JsonDocument doc = JsonDocument.Parse(sourceString))
                {
                    JsonElement root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        // ルート辞書に「prompt」キーがある
                        JsonElement val;
                        string? tmpStr = null;
                        if (root.TryGetProperty("prompt", out val)) tmpStr = val.GetString();
                        if (tmpStr != null) positiveTag = tmpStr;

                        // ルート辞書に「uc」キーがある
                        tmpStr = null;
                        if (root.TryGetProperty("uc", out val)) tmpStr = val.GetString();
                        if (tmpStr != null) negativeTag = tmpStr;

                        // \u00a0を空白に変換
                        return (positiveTag.Replace("\u00a0", " "), negativeTag.Replace("\u00a0", " "));
                    }
                }
            }
            catch { }

            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// 解析モード
        /// </summary>
        enum parseMode : int
        {
            confirming_signature,
            reading_param_len,
            reading_param
        }
        /// <summary>
        /// 隠しPNG情報をチェック
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private static string GetStealthInfo(Image<Rgba32> img)
        {
            string res = string.Empty;
            {
                int width = img.Width;
                int height = img.Height;

                parseMode mode = parseMode.confirming_signature;
                bool isAlpha = false;
                bool isCompress = false;
                int infoLen = 0;

                int byteDataA = 0;
                int byteCheckA = 8;
                List<byte> bufferA = new List<byte>();

                int byteDataRGB = 0;
                int byteCheckRGB = 8;
                List<byte> bufferRGB = new List<byte>();

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // アルファチャネルからbyte列を生成
                        if ((mode == parseMode.confirming_signature) || (isAlpha == true)) 
                        {
                            byteDataA = byteDataA << 1;
                            byteDataA = byteDataA | (img[x, y].A & 1);
                            byteCheckA--;
                            if (byteCheckA == 0)
                            {
                                bufferA.Add((byte)byteDataA);
                                byteCheckA = 8;
                                byteDataA = 0;
                            }
                        }
                        if ((mode == parseMode.confirming_signature) || (isAlpha == true))
                        {
                            // RGBからbyte列を生成
                            List<int> bits = new List<int>();
                            bits.Add((img[x, y].R & 1));
                            bits.Add((img[x, y].G & 1));
                            bits.Add((img[x, y].B & 1));
                            foreach (int bit in bits)
                            {
                                byteDataRGB = byteDataRGB << 1;
                                byteDataRGB = byteDataRGB | bit;
                                byteCheckRGB--;
                                if (byteCheckRGB == 0)
                                {
                                    bufferRGB.Add((byte)byteDataRGB);
                                    byteCheckRGB = 8;
                                    byteDataRGB = 0;
                                }
                            }
                        }

                        switch (mode)
                        {
                            case parseMode.confirming_signature:
                                {
                                    // アルファチャネルに8文字ある
                                    if (bufferA.Count == "stealth_pnginfo".Length)
                                    {
                                        // UTF-8としてでコード
                                        string sig = Encoding.UTF8.GetString(bufferA.ToArray(), 0, bufferA.Count);
                                        if (sig == "stealth_pnginfo")
                                        {
                                            // 通常info
                                            isCompress = false;
                                            isAlpha = true;
                                            mode = parseMode.reading_param_len;
                                            bufferA.Clear();
                                        }
                                        else if (sig == "stealth_pngcomp")
                                        {
                                            // 圧縮info
                                            isCompress = true;
                                            isAlpha = true;
                                            mode = parseMode.reading_param_len;
                                            bufferA.Clear();
                                        }
                                        else
                                        {
                                            // これ以降は無し
                                            return res;
                                        }
                                    }

                                    // RGBに8文字ある
                                    if (bufferRGB.Count == "stealth_pnginfo".Length)
                                    {
                                        // UTF-8としてでコード
                                        string sig = Encoding.UTF8.GetString(bufferRGB.ToArray(), 0, bufferRGB.Count);
                                        if (sig == "stealth_pnginfo")
                                        {
                                            // 通常info
                                            isCompress = false;
                                            isAlpha = false;
                                            mode = parseMode.reading_param_len;
                                            bufferRGB.Clear();
                                        }
                                        else if (sig == "stealth_pngcomp")
                                        {
                                            // 圧縮info
                                            isCompress = true;
                                            isAlpha = false;
                                            mode = parseMode.reading_param_len;
                                            bufferRGB.Clear();
                                        }
                                        else
                                        {
                                            // アルファチャネル側の可能性あり
                                        }
                                    }

                                }
                                break;
                            case parseMode.reading_param_len:
                                {
                                    List<byte> buffer;
                                    if (isAlpha) buffer = bufferA;
                                    else buffer = bufferRGB;

                                    if (buffer.Count == 4)
                                    {
                                        // LE処理系の場合
                                        if (BitConverter.IsLittleEndian) buffer.Reverse();
                                        infoLen = BitConverter.ToInt32(buffer.ToArray(), 0);
                                        if ((isAlpha) && (infoLen < width * height / 8 - 8 - 4))
                                        {
                                            mode = parseMode.reading_param;
                                            buffer.Clear();
                                        }
                                        else if ((isAlpha == false) && (infoLen < width * height * 3 / 8 - 8 - 4))
                                        {
                                            mode = parseMode.reading_param;
                                            buffer.Clear();
                                        }
                                        else
                                        {
                                            return res;
                                        }
                                    }
                                }
                                break;
                            case parseMode.reading_param:
                                {
                                    List<byte> buffer;
                                    if (isAlpha) buffer = bufferA;
                                    else buffer = bufferRGB;

                                    if (buffer.Count == infoLen)
                                    {
                                        if (isCompress)
                                        {
                                            // 圧縮ストリームの場合
                                            using (var memStream = new MemoryStream(buffer.ToArray()))
                                            using (var gzStream = new GZipStream(memStream, CompressionMode.Decompress))
                                            using (var outStream = new MemoryStream())
                                            {
                                                // GZ解凍
                                                int readLen;
                                                int streamLen = 0;
                                                do {
                                                    byte[] tmpBuffer = new byte[4096];
                                                    readLen = gzStream.Read(tmpBuffer, 0, tmpBuffer.Length);
                                                    streamLen += readLen;
                                                    if (readLen > 0)outStream.Write(tmpBuffer, 0, readLen);
                                                } while (readLen != 0);
                                                outStream.Seek(0, SeekOrigin.Begin);

                                                // 文字列へ
                                                res = Encoding.UTF8.GetString(outStream.GetBuffer(), 0, streamLen);
                                                return res;
                                            }
                                        }
                                        else
                                        {
                                            // そのまま文字列へ
                                            res = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count);
                                            return res;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }

            }

            return res;
        }

        /// <summary>
        /// 文字化け対策
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        private static string CheckString(string sourceString)
        {
            // 一旦Byte列に戻す(元のデータが異次元の場合は戻らない)
            byte[] sourceBytes = Encoding.Unicode.GetBytes(sourceString);

            // 文字セットを確認して再エンコード
            var charsetDetectedResult = CharsetDetector.DetectFromBytes(sourceBytes);
            return charsetDetectedResult.Detected.Encoding.GetString(sourceBytes).Replace("\0","");
        }
    }
}
