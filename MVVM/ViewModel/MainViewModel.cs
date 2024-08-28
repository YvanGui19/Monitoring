using Monitoring.Core;
using System;
using System.DirectoryServices;

namespace Monitoring.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand InfoViewCommand { get; set; }

        public HomeViewModel HomeVM { get; set; }
        public InfoViewModel InfoVM { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set 
            { 
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
           HomeVM = new HomeViewModel();
           InfoVM = new InfoViewModel();
           CurrentView = HomeVM;

            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = HomeVM;
            });

            InfoViewCommand = new RelayCommand(o =>
            {
                CurrentView = InfoVM;
            });
        }
    }
}
