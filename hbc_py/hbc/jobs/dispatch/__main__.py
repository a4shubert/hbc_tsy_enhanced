import logging

from hbc.jobs.dispatch import main

logger = logging.getLogger()
if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        logger.info("Exception in hbc.jobs.pipeline")
        raise e
