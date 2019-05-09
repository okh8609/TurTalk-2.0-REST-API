#!/usr/bin/env python
# coding: utf-8

# In[37]:


import requests
import json
url = 'http://localhost:49982/api/account/reg'
headers = {'Content-type': 'application/json'}

response = requests.post(url, data='5555', headers=headers)
print(response.text)


# In[38]:


#測試註冊
import requests
import json
url = 'http://localhost:49982/api/account/reg'
headers = {'Content-type': 'application/json'}

data = { 'email' : "okh8609@gmail.com" , 'password' : '123456789' , 'name' : 'kaihao'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[44]:


#測試登入
import requests
import json
url = 'http://localhost:49982/api/account/login'
headers = {'Content-type': 'application/json'}

data = { 'email' : "okh8609@gmail.com" , 'password' : '123456789'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


key = json.loads(response.text)["Payload"]
print(key)


# In[47]:


#測試權限
import requests
import json

url = 'http://localhost:49982/api/account/test'
hhh = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers = hhh)

print(response.text)


# In[313]:


#測試加入聯絡人

import requests
import json
url = 'http://localhost:49982/api/contacts/add'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : 14}
data_json = json.dumps(data)

response = requests.post(url, data='14', headers = headers)
print(response.text)


# In[311]:


#測試刪除聯絡人

import requests
import json
url = 'http://localhost:49982/api/contacts/del'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}


response = requests.post(url, data='14', headers = headers)
print(response.text)


# In[314]:


#列出聯絡人

import requests
import json
url = 'http://localhost:49982/api/contacts/list'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

response = requests.get(url, headers = headers)
print(response.text)


# In[49]:


#搜尋使用者

import requests
import json
url = 'http://localhost:49982/api/contacts/search/1'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

response = requests.get(url, headers = headers)
print(response.text)


# In[431]:


#送出聊天訊息

import requests
import json
url = 'http://localhost:49982/api/chat/send'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "21" , 'message' : 'qweqwrewrwe'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[387]:


#接收聊天訊息

import requests
import json
url = 'http://localhost:49982/api/chat/receive'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "20" , 'start' : '2019-04-27T23:16:24.264'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[519]:


#送出限時聊天訊息

import requests
import json
import string
url = 'http://localhost:49982/api/chat2/send'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "21" ,
         'message' : "".join(random.sample('abcdefghijklmnopqrstuvwxyz1234567890',35)),  
         "eff_period": "00:00:15.88" }

data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[396]:


#接收限時聊天訊息

import requests
import json
url = 'http://localhost:49982/api/chat2/receive'
headers = {'Authorization': ('Bearer '+ key).encode('utf-8'), 'Content-type': 'application/json'}

data = { 'uid' : "20" , 'start' : '2019-04-28T14:26:06.510'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)


# In[520]:


# 清除過期的聊天訊息
# 應由伺服器端排程去呼叫
# APP端則需自行刪除過期訊息

import requests
import json
url = 'http://localhost:49982/api/chat2/clear'
headers = {'Content-type': 'application/json'}

response = requests.get(url, headers=headers)
print(response.text)


# In[3]:


#測試獲得臨時帳號
import requests
import json

url = 'http://localhost:49982/api/invite/getacc'
hhh = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers = hhh)

print(response.text)

uuu = json.loads(response.text)["uid"]
ppp = json.loads(response.text)["pwd"]


# In[36]:


#測試登入
import requests
import json
url = 'http://localhost:49982/api/invite/login'
headers = {'Content-type': 'application/json'}

data = { 'uid' : '30' , 'pwd' : '5k8QTI3d'}
data_json = json.dumps(data)

response = requests.post(url, data=data_json, headers=headers)
print(response.text)
key = json.loads(response.text)["Payload"]


# In[7]:


#測試權限2
#這裡沒有分使用者角色
import requests
import json

url = 'http://localhost:49982/api/account/test'
hhh = {'Authorization': ('Bearer '+ key).encode('utf-8')}

response = requests.get(url, headers = hhh)

print(response.text)


# In[ ]:




