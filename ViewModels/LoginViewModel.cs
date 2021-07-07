using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using WpfApp.Core;

namespace WpfApp.ViewModels
{
    /// <summary>
    /// Модель представления окна авторизации
    /// </summary>
    [EditorForm(FormType = typeof(LoginWindow), Width = 400, Title ="Автовокзал")]
    public class LoginViewModel : DataObjectBase
    {
        public bool IsAuthenticated { get; set; }

        [Display(Name = "Логин", Order = 1)]
        [Required]
        public string User { get; set; }

        [Display(Name = "Пароль", Order = 2)]
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Проверка логина и пароля
        /// </summary>
        /// <returns></returns>
        string CheckLoginAndPassword()
        {
            if (string.IsNullOrWhiteSpace(User) || string.IsNullOrEmpty(Password))
                return "Необходимо ввести имя пользователя и пароль.";
            if (App.AuthenticationProvider.CheckAutentication(User, Password)) {
                if (App.AuthenticationProvider.Roles.Length == 0) {
                    return "Указанному пользователю не назначена роль в системе. Для возможности работы с системой, необходимо предоставить пользователю роль на уровне базы данных.";
                }
                IsAuthenticated = true;
                return "";
            } else {
                return "Неверное имя пользователя или пароль.";
            }
        }

        public override string Validate() {
            return CheckLoginAndPassword();
        }

        public LoginViewModel() {
            if (System.Diagnostics.Debugger.IsAttached) {
                User = "admin";
                Password = "123";
            }
        }
    }
}
