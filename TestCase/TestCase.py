#!/usr/bin/env python
# coding: utf-8

# In[ ]:


# Just A Request Example...
# Not Have Any Functions...
import requests
import json
url = 'http://localhost:49982/api/account/reg'
headers = {'Content-type': 'application/json'}

response = requests.post(url, data='5555', headers=headers)
print(response.text)


# In[29]:


baseURL = 'http://localhost:49982/'


# In[ ]:


# baseURL = 'https://tt2api.azurewebsites.net/'


# In[24]:


baseURL = 'https://turtalk2api.azurewebsites.net/'


# In[2]:


#測試註冊
import requests
import json
url = baseURL + 'api/account/reg'
headers = {'Content-type': 'application/json'}

data = { 'email' : "okh8609@gmail.com" , 'password' : '123456788' , 'name' : 'kaihao'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[35]:


#測試登入
import requests
import json
url = baseURL + 'api/account/login'
headers = {'Content-type': 'application/json'}

data = { 'email' : "okh8609@gmail.com" , 'password' : '123456789'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


key = json.loads(response.text)["Payload"]
print(key)


# In[ ]:


#測試權限
import requests
import json

url = baseURL + 'api/account/test'
hhh = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers = hhh)

print(response.text)


# In[46]:


#測試取得profile
import requests
import json

url = baseURL + 'api/account/profile'

headers = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers=headers)
print(response.text)


# In[32]:


#測試修改密碼
import requests
import json

url = baseURL + 'api/account/change/passwd'

headers = {'Authorization': ('Bearer '+ key).encode('utf-8'),
           'Content-type': 'application/json'}

data = { 'oldPasswd' : "123456788" , 'newPasswd' : '123456789'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[45]:


#測試修改 name
import requests
import json

url = baseURL + 'api/account/change/name'

headers = {'Authorization': ('Bearer '+ key).encode('utf-8'),
           'Content-type': 'application/json'}

response = requests.post(url, data="\"new.Kaihao\"", headers=headers)
print(response.text)


# In[20]:


#測試加入聯絡人

import requests
import json
url = baseURL + 'api/contacts/add'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'),
           'Content-type': 'application/json'}

response = requests.post(url, data='12', headers = headers)
print(response.text)


# In[9]:


#測試刪除聯絡人

import requests
import json
url = baseURL + 'api/contacts/del'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'),
           'Content-type': 'application/json'}


response = requests.post(url, data='20', headers = headers)
print(response.text)


# In[11]:


#列出聯絡人

import requests
import json
url = baseURL + 'api/contacts/list'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'),
           'Content-type': 'application/json'}

response = requests.get(url, headers = headers)
print(response.text)


# In[23]:


#列出所有使用者

import requests
import json
url = baseURL + 'api/contacts/alluser'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

response = requests.get(url, headers = headers)
print(response.text)


# In[28]:


#搜尋使用者

import requests
import json
keyword = 'kaih'
url = baseURL + 'api/contacts/search/' + keyword
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

response = requests.get(url, headers = headers)
print(response.text)


# In[ ]:


#送出聊天訊息

import requests
import json
url = baseURL + 'api/chat/send'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "21" , 'message' : 'qweqwrewrwe'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[ ]:


#接收聊天訊息

import requests
import json
url = baseURL + 'api/chat/receive'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "20" , 'start' : '2019-04-27T23:16:24.264'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[ ]:


#送出限時聊天訊息

import requests
import json
import string
import random
url = baseURL + 'api/chat2/send'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "21" ,
         'message' : "".join(random.sample('abcdefghijklmnopqrstuvwxyz1234567890',35)),  
         "eff_period": "00:00:15.88" }

data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[ ]:


#接收限時聊天訊息

import requests
import json
url = baseURL + 'api/chat2/receive'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "20" , 'start' : '2019-04-28T14:26:06.510'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[ ]:


# 清除過期的聊天訊息
# 應由伺服器端排程去呼叫
# APP端則需自行刪除過期訊息

import requests
import json
url = baseURL + 'api/chat2/clear'
headers = {'Content-type': 'application/json'}

response = requests.get(url, headers=headers)
print(response.text)


# In[ ]:


#測試獲得臨時帳號
import requests
import json

url = baseURL + 'api/invite/getacc'
hhh = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers = hhh)

print(response.text)

uuu = json.loads(response.text)["uid"]
ppp = json.loads(response.text)["pwd"]


# In[ ]:


#測試登入
import requests
import json
url = baseURL + 'api/invite/login'
headers = {'Content-type': 'application/json'}

data = { 'uid' : '30' , 'pwd' : '5k8QTI3d'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)
key = json.loads(response.text)["Payload"]


# In[ ]:


#測試權限2
#這裡沒有分使用者角色
import requests
import json

url = baseURL + 'api/account/test'
hhh = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers = hhh)

print(response.text)


# In[ ]:





# In[ ]:





# In[ ]:




