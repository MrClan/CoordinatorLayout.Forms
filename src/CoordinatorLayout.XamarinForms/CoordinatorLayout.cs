using System;
using Xamarin.Forms;

namespace CoordinatorLayout.XamarinForms
{
    public partial class CoordinatorLayout : ContentView
    {
        private const string SnapToExtremesAnimation = "SnapToExtremesAnimation";
        private const double ActionViewProportionalHeight = 0.2;

        private readonly RelativeLayout _relativeLayout;
        private View _topView;

        private View _bottomView;
        private BottomViewScrollView _bottomViewContainer;

        private ContentView _actionViewContainer;
        private View _actionView;

        private double _proportionalTopViewHeightMin = 0.1d;
        private double _proportionalTopViewHeightMax = 0.5d;
        private double _proportionalTopViewHeightInitial = 0.0d;
        private double _proportionalTopViewHeight = 0.0d;

        private double _panTotal = 0.0d;

        private PanGestureRecognizer _topViewPanGestureRecognizer;
        private PanGestureRecognizer _bottomViewPanGestureRecognizer;

        private double _bottomViewPreviousTotalY = 0.0d;
        private Direction _bottomViewPanDirection = Direction.none;
        private double _tempPanTotal = 0.0d;
        private double _bottomViewPanDelta = 0.0d;

        private bool _actionViewShowing;

        public CoordinatorLayout()
        {
            _proportionalTopViewHeightInitial = _proportionalTopViewHeightMin;
            _proportionalTopViewHeight = _proportionalTopViewHeightInitial;

            _relativeLayout = new RelativeLayout();

            _bottomViewPanGestureRecognizer = new PanGestureRecognizer();
            _bottomViewPanGestureRecognizer.PanUpdated += BottomViewPanGestureRecognizerOnPanUpdated;
            // _bottomView.GestureRecognizers.Add(_bottomViewPanGestureRecognizer);
            _relativeLayout.GestureRecognizers.Add(_bottomViewPanGestureRecognizer);

            Content = _relativeLayout;
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            if (propertyName == TopViewProperty.PropertyName)
            {
                // Replace all views
                ReplaceTopView();
                ReplaceBottomView();
                ReplaceActionView();
            }

            if (propertyName == BottomViewProperty.PropertyName)
            {
                // Replace only the bottom view (and its container)
                ReplaceBottomView();
            }

            if (propertyName == ActionViewProperty.PropertyName)
            {
                // Replace only the action view (and its container)
                ReplaceActionView();
            }
        }

        private void ReplaceActionView()
        {
            // remove any previous action view and container, if any
            if (_actionView != null)
            {
                _actionViewContainer.Content = null;
                _relativeLayout.Children.Remove(_actionViewContainer);
            }

            // add the new action view, if any and if a top view is available.
            if (ActionView != null && TopView != null)
            {
                // The action view is aaded to a container and that container is added to the relative layout
                _actionViewContainer = new ContentView
                {
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    IsClippedToBounds = false,
                    BackgroundColor = Color.Peru.MultiplyAlpha(0.5d),
                    Content = ActionView
                };

                _relativeLayout.Children.Add(_actionViewContainer,
                    Constraint.Constant(0.0d),
                    Constraint.RelativeToView(_topView, (parent, view) => view.Height - (0.5 * ActionViewProportionalHeight * parent.Height)),
                    Constraint.RelativeToParent(parent => parent.Width),
                    Constraint.RelativeToParent(parent => ActionViewProportionalHeight * parent.Height)
                );

                // remember the new action view
                _actionView = ActionView;
            }
        }

        private void ReplaceBottomView()
        {
            // replace any previous bottom view, if present
            if (_bottomView != null)
            {
                _bottomViewContainer.Content = null;
                _relativeLayout.Children.Remove(_bottomViewContainer);
            }

            // add the new bottom view, if any and if a top view is available.
            if (BottomView != null && TopView != null)
            {
                // the bottom view is added to a container and that container is added to the relative layout
                _bottomViewContainer = new BottomViewScrollView
                {
                    Content = BottomView,
                    InputTransparent = true,
                    CascadeInputTransparent = true,
                    Margin = new Thickness(5)
                };

                _relativeLayout.Children.Add(_bottomViewContainer,
                    Constraint.Constant(0.0d),
                    Constraint.RelativeToView(_topView, (parent, otherView) => otherView.Height + otherView.Margin.VerticalThickness),
                    Constraint.RelativeToParent(parent => parent.Width),
                    Constraint.RelativeToView(_topView, (Parent, otherView) => Parent.Height - otherView.Height)
                );

                // remember the new bottom view
                _bottomView = BottomView;
            }
        }

        private void ReplaceTopView()
        {
            // replace any previous top view, if present
            if (_topView != null)
            {
                _relativeLayout.Children.Remove(_topView);
            }

            // add the new top view, if any
            if (TopView != null)
            {
                _relativeLayout.Children.Add(TopView,
                    Constraint.Constant(0.0d),
                    Constraint.Constant(0.0d),
                    Constraint.RelativeToParent(parent => parent.Width),
                    TopViewHeightConstraint()
                );

                // remember the new top view
                _topView = TopView;
            }
        }

        private Constraint TopViewHeightConstraint()
        {
            return Constraint.RelativeToParent(parent =>
            {
                var topViewHeight = Math.Min(parent.Height * _proportionalTopViewHeightMax, Math.Max(parent.Height * _proportionalTopViewHeightMin, _panTotal));
                _proportionalTopViewHeight = topViewHeight / parent.Height;
                OnTopViewHeightChanged();
                return topViewHeight;
            });
        }

        private void OnTopViewHeightChanged()
        {
            if (_proportionalTopViewHeight <= _proportionalTopViewHeightMin && _actionViewShowing)
            {
                _actionViewShowing = false;
                _actionViewContainer.Content.FadeTo(0.0d, easing: Easing.CubicInOut);
                _actionViewContainer.Content.ScaleTo(0.0d, easing: Easing.CubicInOut);
            }

            if (_proportionalTopViewHeight > _proportionalTopViewHeightMin && !_actionViewShowing)
            {
                _actionViewShowing = true;
                _actionViewContainer.Content.FadeTo(1.0d, easing: Easing.CubicInOut);
                _actionViewContainer.Content.ScaleTo(1.0d, easing: Easing.CubicInOut);
            }
        }


        private async void BottomViewPanGestureRecognizerOnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _bottomViewPreviousTotalY = e.TotalY;

                    this.AbortAnimation(SnapToExtremesAnimation);
                    break;
                case GestureStatus.Running:

                    _bottomViewPanDelta = e.TotalY - _bottomViewPreviousTotalY;
                    _bottomViewPanDirection = _bottomViewPanDelta > 0.0d ? Direction.down : Direction.up;
                    _bottomViewPreviousTotalY = e.TotalY;

                    break;
                case GestureStatus.Completed:
                    OnPanGestureCompleted();
                    break;
                case GestureStatus.Canceled:
                    OnPanGestureCanceled();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Console.WriteLine($"Raw tempPanTotal: {_tempPanTotal}");
            Console.WriteLine($"Direction: {_bottomViewPanDirection}");

            if (e.StatusType == GestureStatus.Running && _topView != null && _bottomView != null)
            {
                if (_bottomViewContainer.ScrollY >= 0.0d && _bottomViewPanDirection == Direction.up && _proportionalTopViewHeight <= _proportionalTopViewHeightMin)
                {
                    var bottomViewScrollY = _bottomViewContainer.ScrollY + (-_bottomViewPanDelta);

                    bottomViewScrollY = Clamp(bottomViewScrollY, 0.0d, _bottomViewContainer.Content.Height - _bottomViewContainer.Height);
                    Console.WriteLine($"Scrolling up to: {bottomViewScrollY}");
                    await _bottomViewContainer.ScrollToAsync(0.0d, bottomViewScrollY, false);
                }
                else if (_bottomViewContainer.ScrollY > 0.0d && _bottomViewPanDirection == Direction.down && _proportionalTopViewHeight <= _proportionalTopViewHeightMin)
                {
                    var bottomViewScrollY = _bottomViewContainer.ScrollY + (-_bottomViewPanDelta);

                    bottomViewScrollY = Clamp(bottomViewScrollY, 0.0d, _bottomViewContainer.Content.Height - _bottomViewContainer.Height);
                    Console.WriteLine($"Scrolling down to: {bottomViewScrollY}");
                    await _bottomViewContainer.ScrollToAsync(0.0d, bottomViewScrollY, false);
                }
                else
                {
                    Console.WriteLine($"Panning {_bottomViewPanDirection} to {_panTotal}");
                    _panTotal += _bottomViewPanDelta;
                    _relativeLayout.ForceLayout();
                }
            }
        }

        private void OnPanGestureCompleted()
        {
            Snap();
        }

        private void OnPanGestureCanceled()
        {
            Snap();
        }

        private void Snap()
        {
            // snap only if top view and bottom view are set
            if (_topView == null || _bottomView == null)
            {
                return;
            }

            // No snap needed if the top view is either completely retracted or fully expanded
            if (Math.Abs(_proportionalTopViewHeight - _proportionalTopViewHeightMin) < double.Epsilon
                || Math.Abs(_proportionalTopViewHeight - _proportionalTopViewHeightMax) < double.Epsilon)
            {
                return;
            }

            // Determine which extreme is closer
            var desiredProportionalTopViewHeight = _proportionalTopViewHeight;
            if (_proportionalTopViewHeight > 0.5d * (_proportionalTopViewHeightMin + (_proportionalTopViewHeightMax - _proportionalTopViewHeightMin)))
            {
                desiredProportionalTopViewHeight = _proportionalTopViewHeightMax;
            }
            else
            {
                desiredProportionalTopViewHeight = _proportionalTopViewHeightMin;
            }

            var snapAnimation = new Animation(d =>
            {
                _panTotal = d * _relativeLayout.Height;
                _relativeLayout.ForceLayout();
            }, _proportionalTopViewHeight, desiredProportionalTopViewHeight, Easing.CubicInOut);
            this.AbortAnimation(SnapToExtremesAnimation);
            snapAnimation.Commit(this, SnapToExtremesAnimation);
        }

        private double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }
    }
}