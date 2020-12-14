using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RecognitionUI
{
    public class Pair<T, U> : INotifyPropertyChanged
    {
        T item1;

        U item2;

        public Pair(T first, U second)
        {
            this.Item1 = first;
            this.Item2 = second;
        }
        public Pair() { }

        public T Item1
        {
            get
            {
                return item1;
            }

            set
            {
                item1 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Classes"));
            }
        }

        public U Item2
        {
            get
            {
                return item2;
            }

            set
            {
                item2 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item2"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
