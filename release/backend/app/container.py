from __future__ import annotations

from dataclasses import dataclass

from app.core.config import Settings
from app.core.events import EventBus
from app.db.repository import SQLiteRepository
from app.services.conversation import ConversationService
from app.services.llm import LlmService
from app.services.planner import PlannerService
from app.services.scheduler import SchedulerService
from app.services.settings import SettingsService
from app.services.speech import SpeechService
from app.services.tasks import TaskService


@dataclass
class AppContainer:
    settings: Settings
    repository: SQLiteRepository
    event_bus: EventBus
    settings_service: SettingsService
    llm_service: LlmService
    speech_service: SpeechService
    task_service: TaskService
    planner_service: PlannerService
    conversation_service: ConversationService
    scheduler_service: SchedulerService


def build_container(settings: Settings) -> AppContainer:
    settings.ensure_directories()
    repository = SQLiteRepository(settings.db_path)
    repository.initialize()
    event_bus = EventBus()
    settings_service = SettingsService(repository, settings)
    llm_service = LlmService(settings)
    speech_service = SpeechService(settings)
    task_service = TaskService(repository, settings)
    planner_service = PlannerService(task_service)
    conversation_service = ConversationService(
        repository=repository,
        task_service=task_service,
        planner_service=planner_service,
        speech_service=speech_service,
        settings_service=settings_service,
        llm_service=llm_service,
        event_bus=event_bus,
    )
    scheduler_service = SchedulerService(repository, event_bus, settings)
    return AppContainer(
        settings=settings,
        repository=repository,
        event_bus=event_bus,
        settings_service=settings_service,
        llm_service=llm_service,
        speech_service=speech_service,
        task_service=task_service,
        planner_service=planner_service,
        conversation_service=conversation_service,
        scheduler_service=scheduler_service,
    )
