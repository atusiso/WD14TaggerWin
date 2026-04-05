using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WD14TaggerWin.ModelManager
{
    /// <summary>
    /// 画像を確保して必要な変換を実施
    /// </summary>
    internal class ImageResizeMethods
    {
        /// <summary>
        /// 正方形イメージに変換(アルファ値は白に合成、元画像を中央に拡大縮小配置) (WD用)
        /// </summary>
        /// <param name="imagePath">画像パス</param>
        /// <param name="size">正方形の辺サイズ</param>
        /// <returns>正方形イメージ</returns>
        public static Image<Rgb24> ConvertSquareImage(Image<Rgba32> source, int size)
        {
            {
                // 元画像の長いほうの辺のサイズで正方形の白画像を生成
                int sqSize = Math.Max(source.Width, source.Height);
                using (Image<Rgba32> img = new(sqSize, sqSize, new Rgba32(255, 255, 255, 255)))
                {
                    // 白画像の上に画像を貼り付け(中央に配置)
                    int padX = 0;
                    int padY = 0;
                    if (source.Width > source.Height) padY = (source.Width - source.Height) / 2;
                    else if (source.Width < source.Height) padX = (source.Height - source.Width) / 2;
                    img.Mutate(ctx => ctx.DrawImage(source, new Point(padX, padY), 1.0f));

                    // RGB24に変換
                    Image<Rgb24> souirceImg = img.CloneAs<Rgb24>();

                    // sizeにサイズ変換
                    if (size > sqSize)
                        // 拡大はbicubicで行う
                        souirceImg.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(size, size),
                            Mode = ResizeMode.Stretch,
                            Sampler = KnownResamplers.Bicubic
                        }));
                    else
                        // 縮小はboxで行う
                        souirceImg.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(size, size),
                            Mode = ResizeMode.Stretch,
                            Sampler = KnownResamplers.Box
                        }));

                    return souirceImg;
                }
            }
        }

        /// <summary>
        /// 画像の短辺を指定のサイズに合わせて拡大縮小する (MLDanbooru用)
        /// </summary>
        /// <param name="imagePath">画像パス</param>
        /// <param name="size">短編の変換後のサイズ</param>
        /// <returns></returns>
        public static Image<Rgb24> ConvertMinsizeImage(Image<Rgba32> source, int size)
        {
            {
                // 白画像を生成
                using (Image<Rgba32> img = new(source.Width, source.Height, new Rgba32(255, 255, 255, 255)))
                {
                    // 白画像の上に画像を貼り付け
                    img.Mutate(ctx => ctx.DrawImage(source, new Point(0, 0), 1.0f));

                    // RGB24に変換
                    Image<Rgb24> souirceImg = img.CloneAs<Rgb24>();

                    // 元画像の低いほうの辺をsizeになるように合わせて縦横を調整(つまり推論対象からはみ出る部分がある…)
                    int minSize = int.Min(source.Width, source.Height);
                    Size resize = new Size((int)(source.Width * size / minSize), (int)(source.Height * size / minSize));
                    resize = new Size(resize.Width & ~3, resize.Height & ~3);
                    souirceImg.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = resize,
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.Lanczos3
                    }));

                    return souirceImg;
                }
            }
        }

        /// <summary>
        /// 正方形イメージに変換(アルファ値は無視、元画像を黒画像の中央に拡大縮小配置) (Carrie用)
        /// </summary>
        /// <param name="imagePath">画像パス</param>
        /// <param name="size">正方形の辺サイズ</param>
        /// <returns>正方形イメージ</returns>
        public static Image<Rgb24> ConvertSquareImage2(Image<Rgba32> sourceARGB, int size)
        {
            // アルファ値は無視
            using (Image<Rgb24> source = sourceARGB.CloneAs<Rgb24>())
            {
                // 元画像の長いほうの辺のサイズで正方形の黒画像を生成
                int sqSize = Math.Max(source.Width, source.Height);
                using (Image<Rgb24> img = new(sqSize, sqSize, new Rgb24(0, 0, 0)))
                {
                    // 黒画像の上に画像を貼り付け(中央に配置)
                    int padX = 0;
                    int padY = 0;
                    if (source.Width > source.Height) padY = (source.Width - source.Height) / 2;
                    else if (source.Width < source.Height) padX = (source.Height - source.Width) / 2;
                    img.Mutate(ctx => ctx.DrawImage(source, new Point(padX, padY), 1.0f));

                    // RGB24に変換
                    Image<Rgb24> souirceImg = img.CloneAs<Rgb24>();

                    // sizeにサイズ変換
                    souirceImg.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(size, size),
                        Mode = ResizeMode.Stretch,
                        Sampler = KnownResamplers.Lanczos3
                    }));

                    return souirceImg;
                }
            }
        }

    }
}
