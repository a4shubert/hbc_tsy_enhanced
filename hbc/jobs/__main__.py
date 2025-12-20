from hbc.jobs import main

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print("Exception in hbc.jobs.pipeline")
        raise e
