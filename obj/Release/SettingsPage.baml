   M S B A M L   `   `   `  ���� ?  ;CCCV, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null1clr-namespace:CCCV.PropertiesCCCV.Properties      CCCV.SettingsPage   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     4
Propertiesclr-namespace:CCCV.Properties       N� Title$  SettingsPage��.��� $��
0,0,-135,0q�$
��Left=�$	��435��.+��� $+��%Настройки программы��$
��Left=�$��40��$��	10,10,0,0q�$	��Top=�$	��335���� $��Как загружать��$
��Left=�$��30��$��	10,50,0,0q�$	��Top=�$	��280��	� -    	�Name$	 RB1��$J��DПри нажатии на всплывающую подсказку��$
��Left=�$��	10,80,0,0q�$	��Top=�$��20��$	��370��	� -   $	 RB2��$g��aАвтоматически, если объем данных не превышает размер��$
��Left=�$��20��$��
10,100,0,0q�$	��Top=�$	��370�� J� RenderTransformOrigin$ 0.568,-0.25&�	� -   $	 RB3��$7��1Автоматически(не более 1ГБ)��$
��Left=�$��20��$��
10,120,0,0q�$	��Top=�$	��370��$ 0.5,-0.5&��� -    ��Name$ IfSizeBigger��$��35��$��10,140,55,0q�$	��Top=�.���� $	��350���� $b�\Загружать при нажатии на всплывающее уведомление,���� �� $k�eесли объём данных превышает максимально установленный���� -    ��Name$ 
SizeOfData��$y�Hidden=�$
��Left=�$��18�� �� TextWrapping$
 Wrap=�$��5��$	��Top=�$��45��$��356,102,0,0q�$ 1.889,-2.056&��� -    ��Name$ Update_Content��$��Обновить��$
��Left=�$��
335,10,0,0q�$	��Top=�$��60��$��28��