import requests
import pg8000.native

#<div class="label-content flex-11">
#<table class="istu-table">

st_accept = "text/html" # говорим веб-серверу, 
                        # что хотим получить html
# имитируем подключение через браузер Mozilla на macOS
st_useragent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 12_3_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.4 Safari/605.1.15"
    # формируем хеш заголовков
headers = {
    "Accept": st_accept,
    "User-Agent": st_useragent
    }

#
#
# Сделаем GET-запрос на сайт кафедры ПО ИЖГТУ и получим html-разметку в виде текста (переменная src)
def GetRequest(link, src):
    # отправляем запрос с заголовками по нужному адресу
    req = requests.get(link, headers)

    # считываем текст HTML-документа
    src = req.text

    return src
#
#
#

#
#
# Поиск ссылок на страницы преподавателей, занесение их в массив all_people_link
def FindTeachers(somebegin, all_people_link, src):
    if(somebegin != -1):
        somebegin += len('<class="not-margin-top asH2">')
        somebegin = src.find('href="', somebegin) + len('href="')
        someend = src.find('"', somebegin)
        all_people_link.append(src[somebegin:someend])
        somebegin = src.find('<div class="not-margin-top asH2">', somebegin)
        FindTeachers(somebegin, all_people_link, src)
    return all_people_link
#
#
#

#
#
# Поиск и заполнение информации о преподавателях
consultations = ['День: ', 'Время начала: ', 'Время окончания: ', 'Аудитория: ', 'Комментарий: ']

def FindInformations(all_people_link, all_people_information):
    for i in range(len(all_people_link)):
        req = requests.get(all_people_link[i], headers)
        src = req.text
        somebegin = src.find('<div class="label-content flex-11">')

        if (somebegin+1):
            all_people_information.append([])

            # Найдём имя преподавателя
            try:
                somebegin = src.index('<title>') + len('<title>')
                someend = src.index(' | ФГБОУ')
                all_people_information[-1].append(src[somebegin:someend])

                # Найдём номер телефона преподавателя
                try:
                    somebegin = src.index('<i class="fa fa-phone">', somebegin)
                    somebegin = src.find('<div>', somebegin) + len('<div>')
                    someend = src.find('</div>', somebegin)
                    all_people_information[-1].append(f'Телефон: {src[somebegin:someend]}')
                except:
                    print(f'Похоже, что {all_people_information[-1][0]} не имеет номера телефона')

                # Найдём номер кабинета преподавателя
                try:
                    somebegin = src.index('<i class="fa fa-map-marker">', somebegin)
                    somebegin = src.find('class="link animation">', somebegin) + len('class="link animation">')
                    someend = src.find('</div>', somebegin)
                    office = src[somebegin:someend].replace('</a>', '')
                    all_people_information[-1].append(f'Кабинет: {office}')
                except:
                    print(f'Похоже, что {all_people_information[-1][0]} не имеет кабинета')

                # Найдём почту преподавателя
                try:
                    somebegin = src.index('<i class="fa fa-envelope">', somebegin)
                    somebegin = src.find('a href="', somebegin)
                    somebegin = src.find('">', somebegin) + 3
                    someend = src.find('</a>', somebegin) - 1
                    all_people_information[-1].append(f'Почта: {src[somebegin:someend]}')
                except:
                    print(f'Похоже, что {all_people_information[-1][0]} не имеет почты')

                # Найдём график консультаций
                try:
                    somebegin = src.index('<table class="istu-table">', somebegin)
                    count = 0
                    one_string_of_table = ''
                    end_of_table = src.find('</table>', somebegin)
                    while (somebegin != 3 and somebegin < end_of_table):
                        somebegin = src.find('<td>', somebegin) + 4
                        someend = src.find('</td>', somebegin)
                        if (somebegin + 1 < someend):
                            one_string_of_table += consultations[count] + src[somebegin:someend].replace('\r\n', '')+ "\n"
                        if (count == 4):
                            all_people_information[-1].append(one_string_of_table)
                            one_string_of_table = ''
                            count = -1
                        count += 1
                except:
                    print(f'Похоже, что {all_people_information[-1][0]} не имеет графика консультаций')

            except:
                print('По этой ссылке не найдено никого под тэгом <title>')
    return all_people_information
#
#
#

# Здесь можно проверить содержимое списка (в нём должны быть ФИО, контакты и консультации преподавателей)
# for people in all_people_information:
#     print()
#     for information in people:
#         print(information)

#
#
#
def ConnectPostgres(all_people_information):
    connection = pg8000.native.Connection('postgres', password = 'line',  host = '26.9.70.246', port = '5432', database = 'tg_bot')

    try:
        connection.run("DROP TABLE IF EXISTS teachers;")
        connection.run("CREATE TABLE teachers (id serial, fullname text NOT NULL, contacts text, consultations text, CONSTRAINT pkey_fullname PRIMARY KEY (fullname));")
        for people in all_people_information:
            contacts = ''
            consultations = ''
            for i in range(1,len(people)):
                if ('Время начала' in people[i]):
                    consultations += people[i]
                else:
                    contacts += people[i] + "\n"
            try:
                connection.run(f"INSERT INTO teachers (fullname, contacts, consultations) VALUES ('{people[0]}', '{contacts}', '{consultations}');")
            except:
                print("ВНИМАНИЕ! Ошибка при добавлении строки в таблицу преподавателей.")
        print('Поздравляем! Таблица преподавателей была успешно обновлена')
    except:
        print('ATTENTION! Неизвестная ошибка при работе с таблицей преподавателей')
    finally:
        connection.close()
#
#
#



def main():
    all_people_link = []
    all_people_information = []
    link_ISTU_PO = "https://istu.ru/department/kafedra-programmnoe-obespechenie#tab-employee"
    src = ''
    try:
        src = GetRequest(link_ISTU_PO, src)
        try:
            somebegin = src.find('<div class="not-margin-top asH2">')
            all_people_link = FindTeachers(somebegin, all_people_link, src)
            try:
                all_people_information = FindInformations(all_people_link, all_people_information)
                try:
                    ConnectPostgres(all_people_information)
                except:
                    print('Ошибка в функции ConnectPostgres()')
            except:
                print('Ошибка в функции FindInformations()')
        except:
            print('Ошибка в функции FindTeachers()')
    except:
        print('Ошибка в функции GetRequest()')

if __name__ == "__main__":
    main()

