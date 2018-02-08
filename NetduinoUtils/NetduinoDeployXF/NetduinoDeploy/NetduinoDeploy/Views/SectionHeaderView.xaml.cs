using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetduinoDeploy
{
	public partial class SectionHeaderView : ContentView
	{
        public bool IsExpanded { get; set; } = true;
        public string Title { get => lblSectionTitle.Text; set => lblSectionTitle.Text = value; }

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
                                                         propertyName: "Title",
                                                         returnType: typeof(string),
                                                         declaringType: typeof(SectionHeaderView),
                                                         defaultValue: "",
                                                         defaultBindingMode: BindingMode.TwoWay,
                                                         propertyChanged: TitlePropertyChanged);

        public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
                                                         propertyName: "IsExpanded",
                                                         returnType: typeof(bool),
                                                         declaringType: typeof(SectionHeaderView),
                                                         defaultValue: true,
                                                         defaultBindingMode: BindingMode.TwoWay,
                                                         propertyChanged: IsExpandedPropertyChanged);

        public SectionHeaderView ()
		{
			InitializeComponent ();

            btnCollapseExpand.Clicked += BtnCollapseExpand_Clicked;
		}

        void BtnCollapseExpand_Clicked(object sender, System.EventArgs e)
        {
            IsExpanded = !IsExpanded;

            btnCollapseExpand.Text = IsExpanded ? "▲" : "▼";

            SetValue(IsExpandedProperty, IsExpanded);
        }

        static void TitlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (SectionHeaderView)bindable;
            control.lblSectionTitle.Text = newValue.ToString();
        }

        static void IsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (SectionHeaderView)bindable;
            control.IsExpanded = (bool)newValue;

            control.btnCollapseExpand.Text = control.IsExpanded ? "▲" : "▼";
        }
    }
}