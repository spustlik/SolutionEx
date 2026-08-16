Problem is that it is needed to recompile TestApp, not .UI
But then xaml doesn't work also...

`d:DataContext="{d:DesignData /_DesignData/ReflectorVM.xaml}"`
* xaml is not working with custom models,
so use

`d:DataContext="{x:Static local:SampleData.MyClasses}">`

