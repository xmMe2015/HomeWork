using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //new List<string> { "c", "a", "d", "b", "e", "b", "a", "b", "c", "d" };
            List<string> pagesjustread = ReadCSV(@"D:\Codes\MySlution\C#\VirtualMemory\PageReplaceAlgorithm\ConsoleApp1\bin\Debug\net5.0\minp1.csv");
            //new List<string> { "c", "A", "d", "B", "e", "b", "A", "b", "c", "d" };
            List<string> pageswithwrite = ReadCSV(@"D:\Codes\MySlution\C#\VirtualMemory\PageReplaceAlgorithm\ConsoleApp1\bin\Debug\net5.0\minp2.csv");
            List<string> frames = new List<string>();

            frames = new List<string> { "a", "b", "c", "d" };
            FIFO(pagesjustread, frames);
            frames = new List<string> { "a", "b", "c", "d" };
            LRU(pagesjustread, frames);
            frames = new List<string> { "10a", "10b", "10c", "10d" };
            SecondChance(pageswithwrite, frames);
            Console.ReadLine();
        }

        static List<string> ReadCSV(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.GetEncoding("UTF-8")))
            {
                while (!reader.EndOfStream)
                {
                    return reader.ReadLine().Split(',').ToList();
                }
            }
            return new List<string>();
        }

        static void FIFO(List<string> pages, List<string> frames)
        {
            Console.WriteLine("FIFO:");
            // 缺页次数
            int pageFaultCount = 0;
            foreach (var page in pages)
            {
                // 如果内存中未找到要访问的页，产生缺页中断
                if (frames.Find(x => x == page.ToLower()) == null)
                {
                    // 页置换
                    frames.Remove(frames[0]);
                    frames.Add(page);
                    pageFaultCount++;
                }
                PrintFrame(frames);
            }
            Console.WriteLine($"pageFaultCount:{pageFaultCount}" + "\n");
        }

        static void LRU(List<string> pages, List<string> frames)
        {
            Console.WriteLine("LRU:");
            // 缺页次数
            int pageFaultCount = 0;
            // 记录访问时间
            Dictionary<string, int> records = new Dictionary<string, int>();
            for (int i = 0; i < pages.Count; i++)
            {
                // 如果内存中未找到要访问的页，产生缺页中断
                if (frames.Find(x => x == pages[i]) == null)
                {
                    // 如果找到空页
                    if (frames.Find(x => x == "") != null)
                    {
                        // 添加最新访问记录
                        records.Add(pages[i], i);
                        // 页置换
                        frames.Remove("");
                        frames.Add(pages[i]);
                    }
                    else
                    {
                        // 移除访问时间最小（即最久未访问的页）
                        var minKey = records.OrderBy(d => d.Value).Select(d => d.Key).FirstOrDefault();
                        records.Remove(minKey);
                        // 添加最新访问记录
                        records.Add(pages[i], i);

                        // 页置换
                        frames.Remove(minKey);
                        frames.Add(pages[i]);
                    }

                    pageFaultCount++;
                }
                // 如果hit，更新最后访问时间
                else
                {
                    if (records.ContainsKey(pages[i]))
                    {
                        records[pages[i]] = i;
                    }
                    else
                    {
                        records.Add(pages[i], i);
                    }
                }
                PrintFrame(frames);
            }
            Console.WriteLine($"pageFaultCount:{pageFaultCount}" + "\n");
        }

        static void SecondChance(List<string> pages, List<string> frames)
        {
            Console.WriteLine("SecondChance:");
            // 缺页次数
            int pageFaultCount = 0;
            // 指针
            int pointer = 0;
            for (int i = 0; i < pages.Count; i++)
            {
                // 如果内存中未找到要访问的页，产生缺页中断
                if (frames.Find(x => x.Contains(pages[i].ToLower())) == null)
                {
                    // 如果找到空页
                    if (frames.Find(x => x == "") != null)
                    {
                        // 换入
                        ReplaceFrame(pages[i], frames, frames.IndexOf(""));
                    }
                    else
                    {
                        // 循环更新used和bit位，知道找到00标记的frame，并移除
                        SwapOut(pages[i], frames, ref pointer);
                    }

                    pageFaultCount++;
                }
                // 如果hit，更新bits
                else
                {
                    for (int j = 0; j < frames.Count; j++)
                    {
                        if (frames[j].Contains(pages[i].ToLower()))
                        {
                            if (Iswrite(pages[i]))
                            {
                                frames[j] = "11" + pages[i].ToLower();
                            }
                            else
                            {
                                frames[j] = "10" + pages[i].ToLower();
                            }
                            break;
                        }
                    }
                }
                PrintFrame(frames);
            }
            Console.WriteLine($"pageFaultCount:{pageFaultCount}" + "\n");
        }

        static void PrintFrame(List<string> frame)
        {
            Console.WriteLine(string.Join('/', frame));
        }

        /// <summary>
        /// 判断是否为写操作（大写）
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        static bool Iswrite(string frame)
        {
            return Regex.IsMatch(frame, @"^[A-Z]+$");
        }

        static void ReplaceFrame(string page, List<string> frames, int count)
        {
            // 添加最新访问记录
            if (Iswrite(page))
            {
                frames[count] = "11" + page.ToLower();
            }
            else
            {
                frames[count] = "10" + page;
            }
        }

        static bool SwapOut(string page, List<string> frames, ref int pointer)
        {
            for (int i = pointer; i < frames.Count; i++)
            {
                // 移除userbit 和 dirtybit 都为0的frame
                if (frames[i].Contains("00"))
                {
                    // 页置换
                    ReplaceFrame(page, frames, i);
                    pointer = i < frames.Count - 1 ? (i + 1) : 0;
                    return true;
                }
                else if (frames[i].Contains("10") || frames[i].Contains("01"))
                {
                    frames[i] = "00" + frames[i].Substring(2);
                }
                else if (frames[i].Contains("11"))
                {
                    frames[i] = "01" + frames[i].Substring(2);
                }
            }
            return SwapOut(page, frames, ref pointer);
        }
    }
}
