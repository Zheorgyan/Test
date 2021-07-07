using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Input;

namespace WpfApp.Core {

    /// <summary>
    /// Вспомогательный класс для отображения окон редакторов объектов
    /// </summary>
    static class UIHelper {

        public static TModel CreateObject<TModel>(Window ownerWindow, DbContext dbContext = null)
            where TModel : DataObjectBase {
            return (TModel)CreateObject(typeof(TModel), ownerWindow, dbContext, true);
        }

        public static DataObjectBase CreateObject(Type dataObjectType, Window ownerWindow, DbContext dbContext = null, bool autoSave = false, Func<object, bool> afterObjectCreated = null) {
            ownerWindow.Cursor = Cursors.Wait;
            DbContext dataContext = (dbContext != null) ? dbContext : App.DataContextFactory.CreateDbContext();
            try {
                if (SecurityHelper.IsCreateAllowed(dataObjectType) == false) {
                    throw new UnauthorizedAccessException(string.Format("Недостаточно прав доступа для создания объекта '{0}'.", ReflectionHelper.GetTypeName(dataObjectType)));
                }
                DataObjectBase model = (DataObjectBase)Activator.CreateInstance(dataObjectType);
                model.InitNewObject(dataContext);
                if (afterObjectCreated != null) {
                    if (!afterObjectCreated(model)) {
                        return null;
                    }
                }
                Window dlg = CreateEditorWindow(model, dataContext);
                dlg.Owner = ownerWindow;
                ArrangeToParentWindow(dlg, ownerWindow);
                dlg.Closing += (sender, e) => {
                    if (dlg.DialogResult == true) {
                        try {
                            dlg.Cursor = Cursors.Wait;
                            string msg = model.Validate();
                            if (!string.IsNullOrEmpty(msg)) {
                                Warning(dlg, msg);
                                e.Cancel = true;
                                return;
                            }
                            model.BeforeAdd(dataContext);
                            dataContext.Add(model);
                            if (autoSave) {
                                dataContext.SaveChanges();
                            }
                        } catch (Exception ex) {
                            Error(dlg, ex);
                            e.Cancel = true;
                        } finally {
                            dlg.Cursor = Cursors.Arrow;
                        }
                    }
                };
                if (dlg.ShowDialog() == true) {
                    return model;
                } else {
                    return null;
                };
            } catch (Exception ex) {
                Error(null, ex);
                return null;

            } finally {
                if (dbContext == null) {
                    dataContext.Dispose();
                }
                ownerWindow.Cursor = Cursors.Arrow;
            }
        }

        public static TModel EditObject<TModel>(int objectId, Window ownerWindow, DbContext dbContext = null)
            where TModel : DataObjectBase {
            ownerWindow.Cursor = Cursors.Wait;
            DbContext dataContext = (dbContext != null) ? dbContext : App.DataContextFactory.CreateDbContext();
            try {
                if (SecurityHelper.IsReadAllowed(typeof(TModel)) == false) {
                    throw new UnauthorizedAccessException(string.Format("Недостаточно прав доступа для просмотра объекта '{0}'.", ReflectionHelper.GetTypeName(typeof(TModel))));
                }
                var model = (TModel)dataContext.Find<TModel>(objectId);
                return (TModel)EditObject(model, ownerWindow, dataContext, true);
            } catch (Exception ex) {
                Error(null, ex);
                return null;
            } finally {
                if (dbContext == null) {
                    dataContext.Dispose();
                }
                ownerWindow.Cursor = Cursors.Arrow;
            }
        }

        public static DataObjectBase EditObject(DataObjectBase model, Window ownerWindow = null, DbContext dbContext = null, bool autoSave = false) {
            if (ownerWindow != null) {
                ownerWindow.Cursor = Cursors.Wait;
            }
            DbContext dataContext = null;
            try {
                if (SecurityHelper.IsReadAllowed(model.GetType()) == false) {
                    throw new UnauthorizedAccessException(string.Format("Недостаточно прав доступа для просмотра объекта '{0}'.", ReflectionHelper.GetTypeName(model.GetType())));
                }
                dataContext = (dbContext != null) ? dbContext : App.DataContextFactory.CreateDbContext();
                model.BeginEdit();
                Window dlg = CreateEditorWindow(model, dataContext);
                if (ownerWindow != null) {
                    dlg.Owner = ownerWindow;
                }
                if (dlg.WindowStartupLocation != WindowStartupLocation.CenterOwner) {
                    ArrangeToParentWindow(dlg, ownerWindow);
                }
                dlg.Closing += (sender, e) => {
                    if (dlg.DialogResult == true) {
                        try {
                            dlg.Cursor = Cursors.Wait;
                            string msg = model.Validate();
                            if (!string.IsNullOrEmpty(msg)) {
                                Warning(dlg, msg);
                                e.Cancel = true;
                                return;
                            }
                            model.BeforeUpdate(dataContext);
                            if (autoSave) {
                                dataContext.SaveChanges();
                            }
                        } catch (Exception ex) {
                            Error(dlg, ex);
                            e.Cancel = true;
                        } finally {
                            dlg.Cursor = Cursors.Arrow;
                        }
                    }
                };
                if (dlg.ShowDialog() == true) {
                    return model;
                } else {
                    model.CancelEdit();
                    return null;
                };
            } catch (Exception ex) {
                Error(null, ex);
                model.CancelEdit();
                return null;
            } finally {
                if (dbContext == null) {
                    dataContext.Dispose();
                }
                if (ownerWindow != null) {
                    ownerWindow.Cursor = Cursors.Arrow;
                }
            }
        }

        public static bool EditObjectWithoutDbContext(DataObjectBase model, Window ownerWindow = null) {
            return (EditObject(model, ownerWindow, null, false) != null);
        }

        public static void EditCollection(Type collectionItemType, Window ownerWindow) {
            ownerWindow.Cursor = Cursors.Wait;
            DbContext dataContext = App.DataContextFactory.CreateDbContext();
            try {
                CollectionEditorWindow dlg = CreateListWindow(collectionItemType, dataContext, false);
                dlg.Owner = ownerWindow;
                ArrangeToParentWindow(dlg, ownerWindow);
                dlg.Closing += (sender, e) => {
                    try {
                        dlg.Cursor = Cursors.Wait;
                    } catch (Exception ex) {
                        Error(dlg, ex);
                        e.Cancel = true;
                    } finally {
                        dlg.Cursor = Cursors.Arrow;
                    }
                };
                dlg.ShowDialog();
            } catch (Exception ex) {
                Error(null, ex);
            } finally {
                dataContext.Dispose();
                ownerWindow.Cursor = Cursors.Arrow;
            }
        }

        public static bool DeleteObject(DataObjectBase model, DbContext dbContext = null, bool autoSave = false) {
            DbContext dataContext = (dbContext != null) ? dbContext : App.DataContextFactory.CreateDbContext();
            try {
                if (SecurityHelper.IsDeleteAllowed(model.GetType()) == false) {
                    throw new UnauthorizedAccessException(string.Format("Недостаточно прав доступа для удаления объекта '{0}'.", ReflectionHelper.GetTypeName(model.GetType())));
                }
                object objectId = ReflectionHelper.GetObjectID(model);
                Type modelType = EFHelper.GetEntityTypeFromProxy(model);
                DataObjectBase obj = (DataObjectBase)dataContext.Find(modelType, objectId);
                if (obj == null) {
                    return true;
                }
                if (MessageBox.Show(string.Format("Удалить '{0}'?", obj), "Удаление записи", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    obj.BeforeDelete(dataContext);
                    dataContext.Remove(obj);
                    if (autoSave) {
                        dataContext.SaveChanges();
                    }
                    return true;
                } else {
                    return false;
                }
            } catch (Exception ex) {
                Error(null, ex);
                return false;
            } finally {
                if (dbContext == null) {
                    dataContext.Dispose();
                }
            }
        }

        public static bool Execute(this Window window, Action action) {
            try {
                window.Cursor = Cursors.Wait;
                action();
                return true;
            } catch(Exception ex) {
                while (ex.InnerException != null) {
                    ex = ex.InnerException;
                }
                Error(window, ex.Message);
                return false;
            } finally {
                window.Cursor = Cursors.Arrow;
            }
        }

        public static void Warning(this Window window, string text, params object[] parameters) {
            text = string.Format(text, parameters);
            MessageBox.Show(text, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void Error(this Window window, Exception ex) {
            while (ex.InnerException != null) {
                ex = ex.InnerException;
            }
            Error(window, string.Join("\r\n", ex.Message));
        }

        public static void Error(this Window window, string text, params object[] parameters) {
            text = string.Format(text, parameters);
            MessageBox.Show(text, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static bool Confirm(this Window window, string text, params object[] parameters) {
            text = string.Format(text, parameters);
            return (MessageBox.Show(text, "Подтверждение операции", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.OK);
        }

        public static int SelectOption(this Window window, string title, params string[] options) {
            return TaskWindow.Show(window, title, options);
        }

        static void ArrangeToParentWindow(Window window, Window parent) {
            if (parent != null) {
                window.Left = parent.Left + 32;
                window.Top = parent.Top + 32;
            }
        }

        static EditorWindow CreateEditorWindow(DataObjectBase model, DbContext dataContext) {
            int width, height;
            string title;
            bool? centerToParent;
            Type editorFormType = ReflectionHelper.GetEditorFormType(model.GetType(), out width, out height, out title, out centerToParent);
            if (editorFormType == null) {
                editorFormType = typeof(EditorWindow);
            }
            var form = (EditorWindow)Activator.CreateInstance(editorFormType, new object[] { dataContext, model });
            if (width > 0) {
                form.SizeToContent = (form.SizeToContent | SizeToContent.Width) ^ SizeToContent.Width;
                form.Width = width;
            }
            if (height > 0) {
                form.SizeToContent = (form.SizeToContent | SizeToContent.Height) ^ SizeToContent.Height;
                form.Height = height;
            }
            if (!string.IsNullOrEmpty(title)) {
                form.Title = title;
            }
            if (centerToParent != null && centerToParent.Value) {
                form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            return form;
        }

        static CollectionEditorWindow CreateListWindow(Type collectionItemType, DbContext dataContext, bool selectionRequired = false) {
            int width, height;
            string title;
            bool? centerToParent;
            Type formType = ReflectionHelper.GetListFormType(collectionItemType, out width, out height, out title, out centerToParent);
            if (formType == null) {
                formType = typeof(CollectionEditorWindow);
            }
            var form = (CollectionEditorWindow)Activator.CreateInstance(formType, new object[] { collectionItemType, dataContext, selectionRequired });
            if (width > 0) {
                form.SizeToContent = (form.SizeToContent | SizeToContent.Width) ^ SizeToContent.Width;
                form.Width = width;
            }
            if (height > 0) {
                form.SizeToContent = (form.SizeToContent | SizeToContent.Height) ^ SizeToContent.Height;
                form.Height = height;
            }
            if (!string.IsNullOrEmpty(title)) {
                form.Title = title;
            }
            if (centerToParent != null && centerToParent.Value) {
                form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            return form;
        }
    }
}
