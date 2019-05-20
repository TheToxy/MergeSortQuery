using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.IO;

using LibraryModel;

namespace MergeSortQuery {
    class MergeSortQuery
    {
        public Library Library { get; set; }
        public int ThreadCount { get; set; }
        delegate bool CopyFilter(Copy copy);
        public List<Copy> ExecuteQuery()
        {
            if (ThreadCount == 0) throw new InvalidOperationException("Threads property not set and default value 0 is not valid.");
            
            var queryList = new List<Copy>(Library.Copies);

            CopyFilter f = (Copy x) =>
            {
                return x.State != CopyState.OnLoan ||
                        x.Book.Shelf[2] > 'Q';
            };

            queryList.RemoveAll(x => f(x));

            // Array version 

            //var filtArray = queryList.ToArray();
            //MergeSort.Sort(filtArray, new BooksComparer(), 1);
            //return new List<Copy>(filtArray);

            // List version
            MergeSort.Sort(queryList, new BooksComparer(), this.ThreadCount - 1);
            return queryList;
        }

        private class MergeSort
        {
            private static void Merge<T>(List<T> list, List<T> left, List<T> right, IComparer<T> comparer)
            {
                int i = 0;
                int j = 0;
                while ((i < left.Count) && (j < right.Count))
                {
                    if (comparer.Compare(left[i], right[j]) < 1)
                    {
                        list[i + j] = left[i];
                        i++;
                    }
                    else
                    {
                        list[i + j] = right[j];
                        j++;
                    }
                }

                if (i < left.Count)
                {
                    while (i < left.Count)
                    {
                        list[i + j] = left[i];
                        i++;
                    }
                }
                else
                {
                    while (j < right.Count)
                    {
                        list[i + j] = right[j];
                        j++;
                    }
                }
            }
            
            public static void Sort<T>(List<T> list, IComparer<T> comparer, int threadCount)
            {
                if (list.Count <= 1)
                    return;
                int center = list.Count / 2;
                List<T> left = new List<T>(center);
                for (int i = 0; i < center; i++)
                    left.Add(list[i]);
                List<T> right = new List<T>(list.Count - center);
                for (int i = center; i < list.Count; i++)
                    right.Add(list[i]);

                if (threadCount > 2)
                {
                    int threads = threadCount / 2;
                    Sort(left, comparer, threadCount - threads);
                    Sort(right, comparer, threads);
                }
                else if (threadCount == 2)
                {
                    Thread thread1 = new Thread(() => left.Sort(comparer));
                    Thread thread2 = new Thread(() => right.Sort(comparer));
                    thread1.Start();
                    thread2.Start();
                    thread1.Join();
                    thread2.Join();
                }
                else if (threadCount == 1)
                {
                    Thread lThread = new Thread(() => left.Sort(comparer));
                    lThread.Start();
                    right.Sort(comparer);
                    lThread.Join();
                }
                else
                {
                    left.Sort(comparer);
                    right.Sort(comparer);
                }

                Merge(list, left, right, comparer);
            }

            public static void Sort<T>(T[] list, IComparer<T> comparer, int threadCount)
            {
                if (list.Length <= 1)
                    return;
                int center = list.Length / 2;
                T[] left = new T[center];
                for (int i = 0; i < center; i++)
                    left[i] = list[i];
                T[] right = new T[list.Length - center];
                for (int i = center; i < list.Length; i++)
                    right[i - center] = list[i];

                if (threadCount > 2)
                {
                    int splitThreads = threadCount / 2;
                    Sort(left, comparer, threadCount - splitThreads);
                    Sort(right, comparer, splitThreads);
                } 
                else if (threadCount == 2)
                {
                    Thread thread1 = new Thread(() => left = QuickSort(left, comparer));
                    Thread thread2 = new Thread(() => right = QuickSort(right, comparer));
                    thread1.Start();
                    thread2.Start();
                    thread1.Join();
                    thread2.Join();
                }
                if (threadCount == 1)
                {
                    Thread lThread = new Thread(() => left = QuickSort(left, comparer));                  
                    lThread.Start();

                    right = QuickSort(right, comparer);

                    lThread.Join();
                }
                else
                {
                    //Sort Left
                    left = QuickSort(left, comparer);
                    //Sort right, 
                    right = QuickSort(right, comparer);
                }

                Merge(list, left, right, comparer);
            }

            private static T[] QuickSort<T>(T[] array, IComparer<T> comparer)
            {
                var tmp2 = new List<T>(array);
                tmp2.Sort(comparer);
                return tmp2.ToArray();
            }

            private static void Merge<T>(T[] list, T[] left, T[] right, IComparer<T> comparer)
            {
                int i = 0;
                int j = 0;

                while ((i < left.Length) && (j < right.Length))
                {
                    if (comparer.Compare(left[i], right[j]) < 1)
                    {
                        list[i + j] = left[i];
                        i++;
                    }
                    else
                    {
                        list[i + j] = right[j];
                        j++;
                    }
                }

                if (i < left.Length)
                {
                    while (i < left.Length)
                    {
                        list[i + j] = left[i];
                        i++;
                    }
                }
                else
                {
                    while (j < right.Length)
                    {
                        list[i + j] = right[j];
                        j++;
                    }
                }
            }
        }
    }

	class ResultVisualizer {
		public static void PrintCopy(StreamWriter writer, Copy c) {
			writer.WriteLine("{0} {1}: {2} loaned to {3}, {4}.", c.OnLoan.DueDate.ToShortDateString(), c.Book.Shelf, c.Id, c.OnLoan.Client.LastName, System.Globalization.StringInfo.GetNextTextElement(c.OnLoan.Client.FirstName));
		}
	}


    

    class BooksComparer : Comparer<Copy>
    {
        // Compares by Date, LastName, FirstName, Shelf, Copy.Id
        public override int Compare(Copy x, Copy y)
        {
            int result = 0;
            if ((result = x.OnLoan.DueDate.CompareTo(y.OnLoan.DueDate)) != 0)
            {
                return result;
            }
            else if ((result = x.OnLoan.Client.LastName.CompareTo(y.OnLoan.Client.LastName)) != 0)
            {
                return result;
            }
            else if ((result = x.OnLoan.Client.FirstName.CompareTo(y.OnLoan.Client.FirstName)) != 0)
            {
                return result;
            }
            else if ((result = x.Book.Shelf.CompareTo(y.Book.Shelf)) != 0)
            {
                return result;
            }
            else if ((result = x.Id.CompareTo(y.Id)) != 0)
            {
                return result;
            }
            else
            {
                return 0;
            }
        }

    }
}
