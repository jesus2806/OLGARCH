using AppGestorVentas.Classes;
using AppGestorVentas.Helpers;
using AppGestorVentas.Models;
using AppGestorVentas.ViewModels.OrdenViewModels;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace AppGestorVentas.Views.OrdenViews
{
    public partial class DatosOrdenView : ContentPage
    {
        public DatosOrdenView(DatosOrdenViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.SetTabBarIsVisible(this, false);
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
            // Cuando la colección cambie, repuebla los productos en UI
            if (viewModel.LstOrdenProducto is INotifyCollectionChanged obs)
            {
                obs.CollectionChanged += (_, __) =>
                {
                    // Asegúrate de ejecutar en el hilo de UI
                    MainThread.BeginInvokeOnMainThread(() => PopulateProductos());
                };
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var vm = (DatosOrdenViewModel)BindingContext;
            await vm.LoadDataApi();
            PopulateProductos();
        }

        private void PopulateProductos()
        {
            slProductos.Children.Clear();
            var vm = (DatosOrdenViewModel)BindingContext;

            foreach (var prod in vm.LstOrdenProducto)
            {
                // ===== Contenedor principal del producto =====
                var border = new Border
                {
                    Stroke = (Color)Resources["Primary500"],
                    StrokeThickness = 1,
                    BackgroundColor = (Color)Resources["Surface"],
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(12)
                };

                var mainStack = new VerticalStackLayout { Spacing = 12 };

                // ===== FILA 1: Nombre + Precio =====
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameLabel = new Label
                {
                    Text = prod.sNombre,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 16,
                    TextColor = (Color)Resources["OnSurface"],
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                headerGrid.Add(nameLabel, 0, 0);

                var priceLabel = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 16,
                    TextColor = (Color)Resources["Primary500"]
                };
                priceLabel.SetBinding(Label.TextProperty,
                    new Binding(nameof(prod.iCostoPublico), source: prod, stringFormat: "${0:N2} MXN"));
                headerGrid.Add(priceLabel, 1, 0);

                mainStack.Add(headerGrid);

                // ===== FILA 2: Controles de cantidad + Botones de acción =====
                var controlsGrid = new Grid { ColumnSpacing = 8 };
                controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Cantidad
                controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // Espacio
                controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Botones

                // --- Controles de cantidad ---
                var cantidadStack = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };

                var btnMenos = new Button
                {
                    Text = "−",
                    FontSize = 20,
                    WidthRequest = 36,
                    HeightRequest = 36,
                    Padding = 0,
                    BackgroundColor = Color.FromArgb("#E0E0E0"),
                    TextColor = Colors.Black,
                    CornerRadius = 18,
                    Command = vm.DecrementarCantidadCommand,
                    CommandParameter = prod
                };
                btnMenos.SetBinding(IsEnabledProperty, new Binding(nameof(vm.BHabilitarAccionesEdicion), source: vm));

                var cantidadLabel = new Label
                {
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    WidthRequest = 40,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                cantidadLabel.SetBinding(Label.TextProperty, new Binding(nameof(prod.iCantidad), source: prod));

                var btnMas = new Button
                {
                    Text = "+",
                    FontSize = 20,
                    WidthRequest = 36,
                    HeightRequest = 36,
                    Padding = 0,
                    BackgroundColor = (Color)Resources["Primary500"],
                    TextColor = Colors.White,
                    CornerRadius = 18,
                    Command = vm.IncrementarCantidadCommand,
                    CommandParameter = prod
                };
                btnMas.SetBinding(IsEnabledProperty, new Binding(nameof(vm.BHabilitarAccionesEdicion), source: vm));

                cantidadStack.Add(btnMenos);
                cantidadStack.Add(cantidadLabel);
                cantidadStack.Add(btnMas);
                controlsGrid.Add(cantidadStack, 0, 0);

                // --- Botones de acción (editar, eliminar) ---
                var accionesStack = new HorizontalStackLayout { Spacing = 8 };

                // Botón Añadir Extra (+ Añadir)
                var btnAnadirExtra = new Button
                {
                    Text = "+ Añadir",
                    FontSize = 12,
                    HeightRequest = 32,
                    Padding = new Thickness(8, 0),
                    BackgroundColor = (Color)Resources["Primary500"],
                    TextColor = Colors.White,
                    CornerRadius = 4,
                    Command = vm.IrAdministrarConsumosCommand,
                    CommandParameter = prod
                };
                btnAnadirExtra.SetBinding(IsVisibleProperty, new Binding(nameof(vm.BHabilitarAccionesEdicion), source: vm));

                // Botón Eliminar
                var btnEliminar = new Button
                {
                    FontFamily = "MaterialDesignIcons",
                    Text = MaterialDesignIcons.Delete,
                    FontSize = 20,
                    WidthRequest = 36,
                    HeightRequest = 36,
                    Padding = 0,
                    BackgroundColor = Color.FromArgb("#B3261E"),
                    TextColor = Colors.White,
                    CornerRadius = 4,
                    Command = vm.EliminarProductoOrdenCommand,
                    CommandParameter = prod.sIdMongo
                };
                btnEliminar.SetBinding(IsVisibleProperty, new Binding(nameof(vm.BHabilitarAccionesEdicion), source: vm));

                accionesStack.Add(btnAnadirExtra);
                accionesStack.Add(btnEliminar);
                controlsGrid.Add(accionesStack, 2, 0);

                mainStack.Add(controlsGrid);

                // ===== FILA 3: Info del producto (expandible) =====
                var expander = new Expander { BindingContext = prod };
                expander.SetBinding(Expander.IsExpandedProperty, new Binding(nameof(prod.IsExpanded), BindingMode.TwoWay));
                expander.ExpandedChanged += OnExpanderExpandedChanged;

                var expanderHeader = new HorizontalStackLayout { Spacing = 4 };
                expanderHeader.Add(new Label
                {
                    Text = "Ver detalles",
                    FontSize = 12,
                    TextColor = (Color)Resources["Primary500"],
                    TextDecorations = TextDecorations.Underline
                });

                expander.Header = expanderHeader;

                // Contenido del expander
                var expanderContent = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(0, 8, 0, 0) };

                // Imagen + Variante
                var imgGrid = new Grid { ColumnSpacing = 12 };
                imgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                imgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var imagen = new Image
                {
                    HeightRequest = 60,
                    WidthRequest = 60,
                    Aspect = Aspect.AspectFill
                };
                imagen.SetBinding(Image.SourceProperty, new Binding(nameof(prod.sURLImagen)));
                imgGrid.Add(imagen, 0, 0);

                var infoStack = new VerticalStackLayout { Spacing = 2 };

                var varianteLabel = new Label
                {
                    FontSize = 13,
                    TextColor = (Color)Resources["OnSurface"]
                };
                varianteLabel.SetBinding(Label.TextProperty,
                    new Binding(".", converter: (IValueConverter)Resources["SelectedVarianteConverter"], source: prod));
                infoStack.Add(varianteLabel);

                if (!string.IsNullOrEmpty(prod.sIndicaciones))
                {
                    infoStack.Add(new Label
                    {
                        Text = $"Indicaciones: {prod.sIndicaciones}",
                        FontSize = 12,
                        FontAttributes = FontAttributes.Italic,
                        TextColor = (Color)Resources["Outline"]
                    });
                }

                imgGrid.Add(infoStack, 1, 0);
                expanderContent.Add(imgGrid);

                // Mostrar consumos con extras si existen
                if (prod.aConsumos != null && prod.aConsumos.Any(c => c.aExtras?.Any() == true))
                {
                    expanderContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
                    expanderContent.Add(new Label
                    {
                        Text = "Extras por consumo:",
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 13,
                        TextColor = (Color)Resources["Primary500"]
                    });

                    foreach (var consumo in prod.aConsumos.Where(c => c.aExtras?.Any() == true))
                    {
                        expanderContent.Add(new Label
                        {
                            Text = $"Consumo {consumo.iIndex}:",
                            FontSize = 12,
                            FontAttributes = FontAttributes.Bold,
                            Margin = new Thickness(8, 4, 0, 0)
                        });

                        foreach (var extra in consumo.aExtras)
                        {
                            var extraLine = new Grid { Margin = new Thickness(16, 0, 0, 0) };
                            extraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                            extraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            extraLine.Add(new Label { Text = $"• {extra.sNombre}", FontSize = 12 }, 0, 0);
                            extraLine.Add(new Label
                            {
                                Text = $"${extra.iCostoPublico:N2}",
                                FontSize = 12,
                                TextColor = (Color)Resources["Primary500"]
                            }, 1, 0);

                            expanderContent.Add(extraLine);
                        }
                    }
                }
                // Fallback: mostrar extras del sistema legacy
                else if (prod.aExtras != null && prod.aExtras.Any())
                {
                    expanderContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
                    expanderContent.Add(new Label
                    {
                        Text = "Extras:",
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 13,
                        TextColor = (Color)Resources["Primary500"]
                    });

                    foreach (var extra in prod.aExtras)
                    {
                        var extraLine = new Grid();
                        extraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                        extraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        extraLine.Add(new Label { Text = $"• {extra.sNombre}", FontSize = 12 }, 0, 0);
                        extraLine.Add(new Label
                        {
                            Text = $"${extra.iCostoPublico:N2}",
                            FontSize = 12,
                            TextColor = (Color)Resources["Primary500"]
                        }, 1, 0);

                        expanderContent.Add(extraLine);
                    }
                }

                // Total del producto
                expanderContent.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });
                var totalGrid = new Grid();
                totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                totalGrid.Add(new Label
                {
                    Text = "TOTAL:",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14
                }, 0, 0);

                var lblTotal = new Label { FontAttributes = FontAttributes.Bold, FontSize = 14, TextColor = (Color)Resources["Primary500"] };
                lblTotal.SetBinding(Label.TextProperty,
                    new Binding(nameof(prod.iTotalGeneralPublicoOrdenProducto), source: prod, stringFormat: "${0:N2}"));
                totalGrid.Add(lblTotal, 1, 0);

                expanderContent.Add(totalGrid);

                expander.Content = expanderContent;
                mainStack.Add(expander);

                border.Content = mainStack;
                slProductos.Children.Add(border);
            }
        }

        private void OnExpanderExpandedChanged(object sender, ExpandedChangedEventArgs e)
        {
            if (!e.IsExpanded) return;

            foreach (var child in slProductos.Children)
            {
                if (child is Border bd && bd.Content is Expander ex && ex != sender)
                    ex.IsExpanded = false;
            }
        }
    }
}
