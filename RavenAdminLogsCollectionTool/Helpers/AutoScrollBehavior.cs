using System;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RavenAdminLogsCollectionTool.Helpers
{
    public class AutoScrollBehavior : Behavior<ScrollViewer>
    {
        public static bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
                if (_isEnabled)
                {
                    _scrollViewer?.ScrollToVerticalOffset((double) _scrollViewer?.ExtentHeight);
                }
            }
        }

        private static ScrollViewer _scrollViewer;
        private static bool _isEnabled;
        private double _height;

        protected override void OnAttached()
        {
            base.OnAttached();

            _scrollViewer = AssociatedObject;
            _scrollViewer.LayoutUpdated += _scrollViewer_LayoutUpdated;
        }

        private void _scrollViewer_LayoutUpdated(object sender, EventArgs e)
        {
            if (IsEnabled && Math.Abs(_scrollViewer.ExtentHeight - _height) > 0.000001)
            {
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.ExtentHeight);
                _height = _scrollViewer.ExtentHeight;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_scrollViewer != null)
            {
                _scrollViewer.LayoutUpdated -= _scrollViewer_LayoutUpdated;
            }
        }
    }
}
