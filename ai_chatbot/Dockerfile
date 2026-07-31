FROM python:3.11-slim
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
WORKDIR /srv
COPY ai_chatbot/requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt
COPY ai_chatbot ./ai_chatbot
RUN mkdir -p /srv/knowledge /srv/generated
EXPOSE 8001
CMD ["uvicorn", "ai_chatbot.app.main:app", "--host", "0.0.0.0", "--port", "8001"]
